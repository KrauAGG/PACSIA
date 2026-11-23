using UnityEngine;
using System.Collections;

public class FlockManager : MonoBehaviour
{
    public GameObject m_FlockPrefab;
    public int m_NumFlocks=20;
    public float m_Limit=1.5f;
    public GameObject[] m_AllFlocks;

    [Header("Flocking Settings")]
    [Range(0.0f, 5.0f)]
    public float m_MinSpeed=0.8f;
    [Range(0.0f, 5.0f)]
    public float m_MaxSpeed=2.5f;
    [Range(1.0f, 10.0f)]
    public float m_NeighbourDistance=1.5f;
    [Range(0.0f, 5.0f)]
    public float m_RotationSpeed=3f;

    [Header("Nest")]
    public float m_SwarmRadius=3f;
    public float m_MinHeight=0.6f;
    [Range(0.0f, 1.0f)]
    public float m_RandomChance=0.1f;
    [Range(0.0f, 5.0f)]
    public float m_SeparationWeight=1f;

    [Header("Leader")]
    public Transform m_Leader;
    [Range(0.0f, 5.0f)]
    public float m_LeaderWeight=1.5f;
    public float m_LeaderSeparation=0.8f;

    //CAMBIOS
    [Header("Goal sin leader")]
    public float m_GoalMinTime=2f;
    public float m_GoalMaxTime=5f;
    Vector3 m_GoalPosition;

    public Vector3 m_Center { get { return m_Leader!=null ? m_Leader.position : m_GoalPosition; } }

    void Start()
    {
        m_GoalPosition=transform.position;
        m_GoalPosition.y=Mathf.Max(m_GoalPosition.y, m_MinHeight + 0.5f);
        StartCoroutine(ChangeGoal());
        Transform l_Container=new GameObject("Nest_" + name).transform;
        m_AllFlocks=new GameObject[m_NumFlocks];
        for (int i=0; i < m_NumFlocks; ++i)
        {
            Vector3 l_Pos=transform.position + new Vector3(Random.Range(-m_Limit, m_Limit), Random.Range(-m_Limit, m_Limit), Random.Range(-m_Limit, m_Limit));
            Vector3 l_Randomize=new Vector3(Random.value * 2 - 1, Random.value * 2 - 1, Random.value * 2 - 1);
            m_AllFlocks[i]=Instantiate(m_FlockPrefab, l_Pos, Quaternion.LookRotation(l_Randomize), l_Container);
            m_AllFlocks[i].GetComponent<Flock>().m_FlockManager=this;
        }
    }

    IEnumerator ChangeGoal()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(m_GoalMinTime, m_GoalMaxTime));

            if (m_Leader==null)
            {
                m_GoalPosition=transform.position + Random.insideUnitSphere*m_SwarmRadius*0.5f;
                m_GoalPosition.y=Mathf.Max(m_GoalPosition.y, m_MinHeight + 0.5f);
            }
        }
    }
}
