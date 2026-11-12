using UnityEngine;

[RequireComponent(typeof(PowerActivator))]
public class DetectGear : MonoBehaviour
{
    public GameObject gearToDetect;
    public GameObject gearVisual;

    private bool complete = false;
    private PowerActivator powerActivator;

    void Awake()
    {
        powerActivator = GetComponent<PowerActivator>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (complete)
            return;


        LaunchHand hand = other.GetComponent<LaunchHand>();
        if (hand != null)
        {
            if (hand.battery.TryGetComponent<gear>(out var Gear))
            {
                hand.DropBattery();

            }

        }

        if (other.gameObject == gearToDetect)
        {
            Destroy(gearToDetect);

            if (gearVisual != null)
                gearVisual.SetActive(true);

            complete = true;

            if (powerActivator != null)
                powerActivator.Activate();
        }
    }
}