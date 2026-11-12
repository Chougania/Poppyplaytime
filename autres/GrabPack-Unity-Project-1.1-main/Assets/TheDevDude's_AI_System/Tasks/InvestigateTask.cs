using UnityEngine;
using UnityEngine.AI;

public class InvestigateTask : AITask
{
    [SerializeField] private float waitTime = 2f;

    private NavMeshAgent agent;

    private bool waiting;
    private float waitTimer;


    public AudioSource huggyAudioSource;
    public AudioClip InvestigateSFX;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public override void Begin()
    {
        base.Begin();

        waiting = false;
        waitTimer = 0f;

        Memory memory = brain.MemorySystem.GetStrongestMemory(MemoryType.Vision);

        if (memory == null)
        {
            memory = brain.MemorySystem.GetStrongestMemory(MemoryType.Sound);
        }

        if (memory != null)
        {
            agent.SetDestination(memory.Position);
        }
        //agent.SetDestination(position);

        huggyAudioSource.loop = false;
        huggyAudioSource.Stop();
        huggyAudioSource.clip = InvestigateSFX;
        huggyAudioSource.Play();
    }

    public override void Tick()
    {
        if (!waiting)
        {
            if (!agent.pathPending &&
                agent.remainingDistance <= agent.stoppingDistance)
            {
                waiting = true;
            }

            return;
        }

        waitTimer += Time.deltaTime;
    }

    public override bool IsComplete()
    {
        return waiting && waitTimer >= waitTime;
    }

    public override void End()
    {
        base.End();

        waitTimer = 0f;
        waiting = false;
    }
}