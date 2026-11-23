using System.Collections;
using UnityEngine;

public class Flock : MonoBehaviour
{
    public FlockManager m_FlockManager;
    [HideInInspector]
    public float m_Speed;
    [HideInInspector]
    public Vector3 m_Direction;

    bool m_Returning;

    void Start()
    {
        m_Speed=Random.Range(m_FlockManager.m_MinSpeed, m_FlockManager.m_MaxSpeed);
        m_Direction=transform.forward*m_Speed;
        StartCoroutine("Flocking");
    }

    void Update()
    {
        Vector3 l_ToCenter=m_FlockManager.m_Center-transform.position;
        float l_Dist=l_ToCenter.magnitude;
        if (l_Dist > m_FlockManager.m_SwarmRadius)
            m_Returning=true;

        else if (l_Dist < m_FlockManager.m_SwarmRadius*0.6f)
            m_Returning=false;

        if (m_Returning)
            m_Direction=l_ToCenter.normalized*m_Speed;

        if (transform.position.y < m_FlockManager.m_MinHeight && m_Direction.y < 0.0f)
            m_Direction.y=-m_Direction.y;

        if (m_Direction.sqrMagnitude > 0.0001f)
            transform.rotation=Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(m_Direction), m_FlockManager.m_RotationSpeed*Time.deltaTime);
        transform.Translate(0.0f, 0.0f, Time.deltaTime*m_Speed);
    }

    IEnumerator Flocking()
    {
        while (true)
        {
            float l_WaitTime=Random.Range(0.3f, 0.5f);
            yield return new WaitForSeconds(l_WaitTime);

            if (m_Returning) continue;
            if (Random.value < m_FlockManager.m_RandomChance)
                m_Speed=Random.Range(m_FlockManager.m_MinSpeed, m_FlockManager.m_MaxSpeed);
            else
                FlockingRules();
        }
    }

    void FlockingRules()
    {
        Vector3 l_Cohesion=Vector3.zero;
        Vector3 l_Separation=Vector3.zero;
        Vector3 l_Align=Vector3.zero;
        int l_Num=0;

        foreach (GameObject l_GameObject in m_FlockManager.m_AllFlocks)
        {
            if (l_GameObject!=gameObject)
            {
                float distance=Vector3.Distance(l_GameObject.transform.position, transform.position);
                if (distance<=m_FlockManager.m_NeighbourDistance && distance>0.0001f)
                {
                    l_Cohesion+=l_GameObject.transform.position;
                    l_Align+=l_GameObject.GetComponent<Flock>().m_Direction;
                    l_Num++;
                    l_Separation-=(transform.position-l_GameObject.transform.position)/(distance*distance);
                }
            }
        }

        Vector3 l_Steer=Vector3.zero;
        if (l_Num > 0)
        {
            l_Align/=l_Num;
            m_Speed=Mathf.Clamp(l_Align.magnitude, m_FlockManager.m_MinSpeed, m_FlockManager.m_MaxSpeed);
            l_Cohesion=(l_Cohesion / l_Num - transform.position).normalized*m_Speed;

            l_Steer=l_Cohesion + l_Align + l_Separation*m_FlockManager.m_SeparationWeight;
        }

        if (m_FlockManager.m_Leader != null)
        {
            Vector3 l_ToLeader=m_FlockManager.m_Leader.position - transform.position;
            Vector3 l_Follow=l_ToLeader.normalized*m_Speed*m_FlockManager.m_LeaderWeight;
            if (l_ToLeader.magnitude < m_FlockManager.m_LeaderSeparation)
                l_Follow=-l_Follow;
            l_Steer += l_Follow;
        }

        if (l_Steer.sqrMagnitude > 0.0001f)
            m_Direction=l_Steer.normalized*m_Speed;
    }
}
