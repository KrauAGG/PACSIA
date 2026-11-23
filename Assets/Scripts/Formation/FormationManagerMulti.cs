using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public class FormationManagerMulti : MonoBehaviour
{
    [Header("Lider")]
    public Transform m_Leader;
    public string m_RunningBoolName="Running";

    [Header("Seguidors")]
    public GameObject m_FollowerPrefab;
    public int m_NumFollowers=4;
    public List<FormationFollower> m_Followers=new List<FormationFollower>();

    [Header("Formació")]
    public float m_SpaceBetweenUnits=2f;
     public int m_Columns=2;

    public enum FormationType { Patrol, DefensiveCircle }

    [Header("Cercle defensiu")]
    public float m_MinCircleRadius=2f;

    [Header("Canvi de formació")]
    public string m_ThreatBoolName="Running";
    public float m_SwitchDelay=0.5f;
    public float m_MinTimeInFormation=1.5f;

    FormationType m_CurrentFormation=FormationType.Patrol;
    float m_FormationTimer;
    float m_PendingTimer;
    int m_ThreatHash;

    NavMeshAgent m_LeaderNavMeshAgent;
    Animator m_LeaderAnimator;
    bool m_LeaderIsRunning;
    int m_RunningHash;


    void Start()
    {
        m_LeaderNavMeshAgent=m_Leader.GetComponent<NavMeshAgent>();
        m_LeaderAnimator=m_Leader.GetComponent<Animator>();
        m_RunningHash=Animator.StringToHash(m_RunningBoolName);
        m_ThreatHash=Animator.StringToHash(m_ThreatBoolName);
        m_LeaderIsRunning=false;

        if (m_Followers.Count==0 && m_FollowerPrefab!=null)
        {
            for (int i=0; i < m_NumFollowers; ++i)
            {
                GameObject l_GameObject=Instantiate(m_FollowerPrefab, m_Leader.position, m_Leader.rotation);
                l_GameObject.name=m_FollowerPrefab.name + "_" + i;
                m_Followers.Add(l_GameObject.GetComponent<FormationFollower>());
            }
        }
        AssignSlots(true);
    }

    void Update()
    {
        if(m_LeaderAnimator==null)
            return;
        
        if(m_LeaderAnimator.GetBool(m_RunningHash)!=m_LeaderIsRunning)
        {
            m_LeaderIsRunning=m_LeaderAnimator.GetBool(m_RunningHash);
            foreach(FormationFollower l_Follower in m_Followers)
                l_Follower.ChangeRunningState(m_LeaderIsRunning);
        }
        UpdateFormation(m_LeaderAnimator.GetBool(m_ThreatHash));
    }

    public void AssignSlots(bool Teleport)
    {
        int l_FollowersQty=m_Followers.Count;
        if(!Teleport)
            ReorderFollowersByProximity();
        for (int i=0; i < l_FollowersQty; ++i)
        {
            ComputeSlot(i, out Vector3 l_Offset, out float l_Yaw);
            m_Followers[i].SetSlot(m_Leader, m_LeaderNavMeshAgent, l_Offset, l_Yaw, Teleport);
            m_Followers[i].GetComponent<NavMeshAgent>().avoidancePriority=Mathf.Clamp(51 + i, 0, 99);
        }
    }

    public void ComputeSlot(int i, out Vector3 Offset, out float Yaw)
    {
        Yaw=0f;

        if(m_CurrentFormation==FormationType.DefensiveCircle)
        {
            int l_Count=Mathf.Max(1, m_Followers.Count);
            float l_Radius=Mathf.Max(m_MinCircleRadius, l_Count*m_SpaceBetweenUnits/(2f*Mathf.PI));
            float l_Angle=i*2f*Mathf.PI/l_Count;
            Offset=new Vector3(Mathf.Sin(l_Angle), 0f, Mathf.Cos(l_Angle))*l_Radius;
            Yaw=l_Angle*Mathf.Rad2Deg;
            return;
        }

        int l_Row=i / m_Columns;
        int l_Col=i % m_Columns;
        float l_X=(l_Col - (m_Columns - 1) * 0.5f) * m_SpaceBetweenUnits;
        Offset=new Vector3(l_X, 0f, -(l_Row + 1) * m_SpaceBetweenUnits);
    }

    void UpdateFormation(bool Threatened)
    {
        m_FormationTimer+=Time.deltaTime;
        FormationType l_Desired=Threatened ? FormationType.DefensiveCircle : FormationType.Patrol;

        if(l_Desired==m_CurrentFormation)
        {
            m_PendingTimer=0f;
            return;
        }

        m_PendingTimer+=Time.deltaTime;
        if(m_PendingTimer>=m_SwitchDelay && m_FormationTimer>=m_MinTimeInFormation)
            ChangeFormation(l_Desired);
    }
    public void ChangeFormation(FormationType NewFormation)
    {
        if(m_CurrentFormation==NewFormation)
            return;
        m_CurrentFormation=NewFormation;
        m_FormationTimer=0f;
        m_PendingTimer=0f;
        AssignSlots(false);
    }
    void ReorderFollowersByProximity()
    {
        int l_Count=m_Followers.Count;
        if(l_Count<2)
            return;

        Quaternion l_LeaderYaw=Quaternion.Euler(0f, m_Leader.eulerAngles.y, 0f);
        Vector3[] l_Slots=new Vector3[l_Count];
        for(int i=0; i<l_Count; ++i)
        {
            ComputeSlot(i, out Vector3 l_Offset, out float l_Yaw);
            l_Slots[i]=m_Leader.position + l_LeaderYaw*l_Offset;
        }

        int[] l_SlotOfFollower=new int[l_Count];
        bool[] l_SlotTaken=new bool[l_Count];
        for(int i=0; i<l_Count; ++i)
            l_SlotOfFollower[i]=-1;

        List<(float Dist, int Follower, int Slot)> l_Pairs=new List<(float, int, int)>();
        for(int f=0; f<l_Count; ++f)
            for(int s=0; s<l_Count; ++s)
                l_Pairs.Add((FlatDistance(m_Followers[f].transform.position, l_Slots[s]), f, s));
        l_Pairs.Sort((a, b) => a.Dist.CompareTo(b.Dist));

        foreach(var l_Pair in l_Pairs)
        {
            if(l_SlotOfFollower[l_Pair.Follower]!=-1 || l_SlotTaken[l_Pair.Slot])
                continue;
            l_SlotOfFollower[l_Pair.Follower]=l_Pair.Slot;
            l_SlotTaken[l_Pair.Slot]=true;
        }

        bool l_Improved=true;
        int l_Iterations=0;
        while(l_Improved && l_Iterations<l_Count*l_Count)
        {
            l_Improved=false;
            ++l_Iterations;
            for(int a=0; a<l_Count-1; ++a)
            {
                for(int b=a+1; b<l_Count; ++b)
                {
                    Vector3 l_PosA=m_Followers[a].transform.position;
                    Vector3 l_PosB=m_Followers[b].transform.position;
                    int l_SlotA=l_SlotOfFollower[a];
                    int l_SlotB=l_SlotOfFollower[b];
                    float l_Current=FlatDistance(l_PosA, l_Slots[l_SlotA]) + FlatDistance(l_PosB, l_Slots[l_SlotB]);
                    float l_Swapped=FlatDistance(l_PosA, l_Slots[l_SlotB]) + FlatDistance(l_PosB, l_Slots[l_SlotA]);
                    if(l_Swapped + 0.001f < l_Current)
                    {
                        l_SlotOfFollower[a]=l_SlotB;
                        l_SlotOfFollower[b]=l_SlotA;
                        l_Improved=true;
                    }
                }
            }
        }

        FormationFollower[] l_Ordered=new FormationFollower[l_Count];
        for(int f=0; f<l_Count; ++f)
            l_Ordered[l_SlotOfFollower[f]]=m_Followers[f];
        m_Followers.Clear();
        m_Followers.AddRange(l_Ordered);
    }
    float FlatDistance(Vector3 A, Vector3 B)
    {
        A.y=0f;
        B.y=0f;
        return Vector3.Distance(A, B);
    }
}
