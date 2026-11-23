using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public class FormationManager : MonoBehaviour
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

    NavMeshAgent m_LeaderNavMeshAgent;
    Animator m_LeaderAnimator;
    bool m_LeaderIsRunning;
    int m_RunningHash;


    void Start()
    {
        m_LeaderNavMeshAgent=m_Leader.GetComponent<NavMeshAgent>();
        m_LeaderAnimator=m_Leader.GetComponent<Animator>();
        m_RunningHash=Animator.StringToHash(m_RunningBoolName);
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
    }

    public void AssignSlots(bool Teleport)
    {
        int l_FollowersQty=m_Followers.Count;
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

        int l_Row=i / m_Columns;
        int l_Col=i % m_Columns;
        float l_X=(l_Col - (m_Columns - 1) * 0.5f) * m_SpaceBetweenUnits;
        Offset=new Vector3(l_X, 0f, -(l_Row + 1) * m_SpaceBetweenUnits);

    }
}
