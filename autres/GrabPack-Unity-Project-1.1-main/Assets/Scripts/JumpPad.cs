using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class JumpPad : MonoBehaviour
{
    public float jumpForce = 10f;
    public float maxBoostDistance = 10f;
    public float minBoostMultiplier = 0.2f;
    public float cooldownTime = 2f;

    public AudioClip boostSFX;

    private AudioSource audioSource;
    private float cooldownTimer;


    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

    }

    void Update()
    {
        cooldownTimer -= Time.deltaTime;
    }

    void OnTransformChildrenChanged()
    {
        if (!enabled)
            return;

        if (cooldownTimer > 0)
            return;

        LaunchHand hand = GetComponentInChildren<LaunchHand>();
        if (hand == null)
            return;

        if (!hand.isRocketHand)
            return;

        Rigidbody playerRb = hand.ownerRigidbody;
        RigidboyPlayerController player = hand.ownerController;

        if (playerRb == null)
            return;

        float baseForce = player != null && player.isGrounded ? jumpForce : jumpForce / 2f;

        float distance = Vector3.Distance(playerRb.position, transform.position);
        float distanceMultiplier = Mathf.Clamp01(1 - (distance / maxBoostDistance));
        distanceMultiplier = Mathf.Lerp(minBoostMultiplier, 1f, distanceMultiplier);

        float finalForce = baseForce * distanceMultiplier;

        playerRb.linearVelocity = Vector3.zero;
        playerRb.AddForce(transform.up * finalForce, ForceMode.Impulse);

        hand.return1();

        if (boostSFX != null)
            audioSource.PlayOneShot(boostSFX);

        cooldownTimer = cooldownTime;
    }
}