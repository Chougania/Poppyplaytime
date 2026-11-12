using UnityEngine;

public class AIMovementStateTrigger : MonoBehaviour
{

    public Animator AIAnims;
    public string AnimatorState = "Crouching";

    public void OnTriggerEnter(Collider other)
    {
        if (other.tag == "AI")
        {
            AIAnims.SetBool(AnimatorState, true);
        }
    }
    public void OnTriggerExit(Collider other)
    {
        if (other.tag == "AI")
        {
            AIAnims.SetBool(AnimatorState, false);
        }
    }
}
