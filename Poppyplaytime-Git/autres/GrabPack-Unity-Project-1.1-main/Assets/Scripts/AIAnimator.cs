using UnityEngine;
using UnityEngine.AI;

public class AIAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int CrouchingHash = Animator.StringToHash("Crouching");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int JumpscareHash = Animator.StringToHash("Jumpscare");
    private static readonly int RoarHash = Animator.StringToHash("Roar");

    [SerializeField] private float maxSpeed = 8f;



    private void Update()
    {
        float normalizedSpeed = agent.velocity.magnitude / maxSpeed;

        animator.SetFloat(SpeedHash, normalizedSpeed);
    }

    public void SetCrouching(bool value)
    {
        animator.SetBool(CrouchingHash, value);
    }

    public void Jump()
    {
        animator.SetTrigger(JumpHash);
    }

    public void Jumpscare()
    {
        animator.SetTrigger(JumpscareHash);
    }

    public void Roar()
    {
        animator.SetTrigger(RoarHash);
    }
}