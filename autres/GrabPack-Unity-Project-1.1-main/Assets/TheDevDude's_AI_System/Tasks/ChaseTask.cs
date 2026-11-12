using UnityEngine;
using UnityEngine.AI;

public class ChaseTask : AITask
{
    private NavMeshAgent agent;

    [SerializeField] private float minSpeed = 3.5f;
    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private float accelerationTime = 4f;

    private float chaseTimer;

    public AudioSource AudioSource;
    public AudioSource AdditionalAudioSource;

    public AudioClip AlertSFX;
    public AudioClip ChaseSFX;

    [SerializeField] private float ambushChance = 0.2f;
    private bool sawPlayerLastFrame;
    public override void Begin()
    {
        base.Begin();
        brain.Blackboard.Set(AIKeys.ForceChase, false);
        chaseTimer = 0f;
        agent.speed = minSpeed;

        sawPlayerLastFrame = brain.Blackboard.Get<bool>(AIKeys.CanSeeTarget);

        AudioSource.loop = false;
        AudioSource.Stop();
        AudioSource.clip = AlertSFX;
        AudioSource.Play();

        AdditionalAudioSource.loop = true;
        AdditionalAudioSource.Stop();
        AdditionalAudioSource.clip = ChaseSFX;
        AdditionalAudioSource.Play();
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public override void Tick()
    {
        bool canSeeTarget = brain.Blackboard.Get<bool>(AIKeys.CanSeeTarget);

        if (sawPlayerLastFrame && !canSeeTarget)
        {
            if (Random.value < ambushChance)
            {
                brain.Blackboard.Set(AIKeys.IsAmbushing, true);
                sawPlayerLastFrame = false;
                return;
            }
        }

        sawPlayerLastFrame = canSeeTarget;

        //Debug.Log("Chase Tick");
        chaseTimer += Time.deltaTime;

        float t = Mathf.Clamp01(chaseTimer / accelerationTime);
        agent.speed = Mathf.Lerp(minSpeed, maxSpeed, t);

        Transform target = brain.Blackboard.Get<Transform>(AIKeys.Target);

        if (target != null)
        {
            NavMeshPath path = new NavMeshPath();

            if (agent.CalculatePath(target.position, path))
            {
                if (path.status != NavMeshPathStatus.PathInvalid)
                {
                    agent.SetPath(path);
                }
            }
        }
    }

    public override void End()
    {
        base.End();
        AdditionalAudioSource.Stop();

        agent.ResetPath();
    }


}