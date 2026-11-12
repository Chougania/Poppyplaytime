using UnityEngine;
using UnityEngine.AI;

public class PatrolTask : AITask
{
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolSpeed = 3.5f;

    private NavMeshAgent agent;
    private int currentPoint;


    public AudioSource huggyAudioSource;
    public AudioClip PatrolSFX;

    [SerializeField] private float ambushChance = 0.25f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public override void Begin()
    {
        agent.speed = patrolSpeed;

        huggyAudioSource.loop = true;
        huggyAudioSource.Stop();
        huggyAudioSource.clip = PatrolSFX;
        huggyAudioSource.Play();

        GoToNextPoint();
    }

    public override void Tick()
    {
        if (patrolPoints.Length == 0)
            return;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (Random.value < ambushChance)
            {
                brain.Blackboard.Set(AIKeys.IsAmbushing, true);
                return;
            }

            GoToNextPoint();
        }
    }

    private void GoToNextPoint()
    {
        if (patrolPoints.Length == 0)
            return;

        if (patrolPoints.Length == 1)
        {
            currentPoint = 0;
        }
        else
        {
            int newPoint;
            do
            {
                newPoint = Random.Range(0, patrolPoints.Length);
            }
            while (newPoint == currentPoint);

            currentPoint = newPoint;
        }

        agent.SetDestination(patrolPoints[currentPoint].position);
    }
}