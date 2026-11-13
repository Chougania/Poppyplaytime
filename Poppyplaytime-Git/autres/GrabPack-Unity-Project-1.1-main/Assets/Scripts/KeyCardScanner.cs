using UnityEngine;

[RequireComponent(typeof(PowerActivator))]
public class KeyCardScanner : MonoBehaviour
{
    public Animator animator;
    public KeyCard connectedCard;

    private PowerActivator powerActivator;

    void Awake()
    {
        powerActivator = GetComponent<PowerActivator>();
    }


    public void Insert()
    {
        if (connectedCard != null && connectedCard.PICKED)
        {

            if (animator != null)
                animator.SetBool("insert", true);

            if (powerActivator != null)
                powerActivator.Activate();
        }

    }
}