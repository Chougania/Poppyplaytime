using UnityEngine;
using UnityEngine.AI;

public class AIFootsteps : MonoBehaviour
{
    public AudioSource FootstepsAudioSource;
    public AudioClip FootstepClip;

    public float minVelocity = 0.1f;
    public float Speed = 3.5f;

    public float minPitch = 0.9f;
    public float maxPitch = 1.1f;

    private NavMeshAgent agent;

    public float stepTimer;



    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

    }

    void Update()
    {
        float currentSpeed = agent.desiredVelocity.magnitude;
        stepTimer -= Time.deltaTime;

        if (currentSpeed > minVelocity)
        {
            float speedFactor = Mathf.Max(currentSpeed / Speed, 0.2f);
            FootstepsAudioSource.pitch = Mathf.Clamp(speedFactor, minPitch, maxPitch);

           // Debug.Log(speedFactor);
            if (stepTimer <= 0f)
            {
                FootstepsAudioSource.PlayOneShot(FootstepClip, 1.0f);
                stepTimer = 1f / speedFactor;
            }
        }

    }
}
