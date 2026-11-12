using UnityEngine;

[RequireComponent(typeof(PowerActivator))]
public class BatterySOcket : MonoBehaviour
{
    [SerializeField] private LayerMask batteryLayer;
    [SerializeField] private Transform batterySnapPoint;

    private PowerActivator powerActivator;

    private Collider currentBattery;
    private Rigidbody batteryRb;

    public bool IsFull => currentBattery != null;

    void Awake()
    {
        powerActivator = GetComponent<PowerActivator>();
    }

    void OnTriggerStay(Collider other)
    {
        if (currentBattery != null)
            return;

        LaunchHand hand = other.GetComponent<LaunchHand>();
        if (hand != null)
        {
            hand.DropBattery();

        }

        if ((batteryLayer & (1 << other.gameObject.layer)) == 0)
            return;

        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb == null)
            return;

        InsertBattery(other, rb);
    }

    void OnTriggerExit(Collider other)
    {
        if (other == currentBattery)
        {
            RemoveBattery();
        }
    }

    void LateUpdate()
    {
        if (currentBattery == null)
            return;

        if (currentBattery.transform.position != batterySnapPoint.position)
        {
            RemoveBattery();
        }
    }

    void InsertBattery(Collider battery, Rigidbody rb)
    {
        currentBattery = battery;
        batteryRb = rb;

        rb.isKinematic = true;

        battery.transform.position = batterySnapPoint.position;
        battery.transform.rotation = batterySnapPoint.rotation;

        powerActivator.Activate();
    }

    void RemoveBattery()
    {


        currentBattery = null;
        batteryRb = null;

        powerActivator.Deactivate();
    }
}