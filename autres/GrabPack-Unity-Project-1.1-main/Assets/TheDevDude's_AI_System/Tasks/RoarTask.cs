using UnityEngine;
using UnityEngine.AI;

public class RoarTask : AITask
{
    [SerializeField] private float turnSpeed = 360f;
    public override bool CanInterrupt => false;
    private NavMeshAgent agent;
    private AIAnimator aiAnimator;

    private float timer;

    public AudioSource AudioSource;
    public AudioClip AlertSFX;

    public AudioSource AdditionalAudioSource;
    public AudioClip StartSFX;


    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        aiAnimator = GetComponent<AIAnimator>();

    }

    public override void Begin()
    {
        base.Begin();

        brain.Blackboard.Set(AIKeys.HasRoared, true);
        brain.Blackboard.Set(AIKeys.ShouldRoar, false);
        agent.isStopped = true;

        agent.ResetPath();
        agent.isStopped = true;
        //Debug.Log(agent.isStopped);
        aiAnimator.Roar();

        AdditionalAudioSource.loop = false;
        AdditionalAudioSource.Stop();
        AdditionalAudioSource.clip = StartSFX;
        AdditionalAudioSource.Play();

    }

    public override void Tick()
    {
        timer += Time.deltaTime;
        Debug.Log("Roar Tick");
        Transform target = brain.Blackboard.Get<Transform>(AIKeys.Target);

        if (target != null)
        {
            Vector3 direction = target.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);

                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    turnSpeed * Time.deltaTime
                );
            }
        }

    }

    public void RoarSFX()
    {
        AudioSource.loop = false;
        AudioSource.Stop();
        AudioSource.clip = AlertSFX;
        AudioSource.Play();
    }

    public void RoarFinished()
    {
        Complete();
    }


    public override void End()
    {
        base.End();
        agent.isStopped = false;
    }
}