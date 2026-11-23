using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class FormationFollower : MonoBehaviour
{
    [Header("Slot")]
    public Transform m_FollowingTo;
    public Vector3 m_Offset;
    public float m_Yaw;

    [Header("Moviment")]
    public float m_UpdateFreq=0.2f;
    public float m_SampleRadius=2f;
    public float m_CatchUpDistance=3f;
    public float m_CatchUpSpeedMultiplier=1.5f;
    public float m_ArriveDistance=0.3f;
    public float m_TurnSpeed=5f;

    NavMeshAgent m_NavMeshAgent;
    Animator m_Animator;
    NavMeshAgent m_SpeedSource;
    float m_BaseSpeed;
    float m_Freq;

    void Awake()
    {
        m_NavMeshAgent=GetComponent<NavMeshAgent>();
        m_Animator=GetComponent<Animator>();
        m_BaseSpeed=m_NavMeshAgent.speed;
    }

    public void SetSlot(Transform NewAnchor, NavMeshAgent NewSpeedSource, Vector3 Offset, float Yaw, bool Teleport)
    {
        m_FollowingTo=NewAnchor;
        m_SpeedSource=NewSpeedSource;
        m_Offset=Offset;
        m_Yaw=Yaw;

        if (Teleport)
        {
            m_NavMeshAgent.Warp(SlotPosition());
            transform.rotation=SlotRotation();
        }
        m_Freq=m_UpdateFreq;
    }

    Quaternion AnchorYaw()
    {
        return Quaternion.Euler(0f, m_FollowingTo.eulerAngles.y, 0f);
    }

    public Vector3 SlotPosition()
    {
        return m_FollowingTo.position + AnchorYaw() * m_Offset;
    }

    public Quaternion SlotRotation()
    {
        return AnchorYaw() * Quaternion.Euler(0f, m_Yaw, 0f);
    }

    void Update()
    {
        if (m_FollowingTo == null) 
            return;

        m_Freq += Time.deltaTime;
        if (m_Freq >= m_UpdateFreq)
        {
            m_Freq=0f;
            GoToSlot();
        }

        if (!m_NavMeshAgent.pathPending && m_NavMeshAgent.remainingDistance <= m_ArriveDistance)
            transform.rotation=Quaternion.Slerp(transform.rotation, SlotRotation(), m_TurnSpeed * Time.deltaTime);
    }

    void GoToSlot()
    {
        Vector3 l_Target=SlotPosition();

        if (!NavMesh.SamplePosition(l_Target, out NavMeshHit hit, m_SampleRadius, NavMesh.AllAreas))
            return;
        m_NavMeshAgent.destination=hit.position;

        float l_Reference=m_SpeedSource != null ? m_SpeedSource.speed : m_BaseSpeed;
        float l_Distance=Vector3.Distance(transform.position, hit.position);
        m_NavMeshAgent.speed=l_Distance > m_CatchUpDistance ? l_Reference * m_CatchUpSpeedMultiplier : l_Reference;
    }

    public void ChangeRunningState(bool IsRunning)
    {
        m_NavMeshAgent.speed=m_SpeedSource.speed;
        m_Animator.SetBool("Running", IsRunning);
    }
}
