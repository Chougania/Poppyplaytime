using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsButton : MonoBehaviour
{
    private HashSet<Rigidbody> objectsInTrigger = new HashSet<Rigidbody>();
    private PowerActivator powerActivator;

    public bool HasObjects => objectsInTrigger.Count > 0;

    private bool wasPressed = false; 


    void Awake()
    {
        powerActivator = GetComponent<PowerActivator>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;

        if (rb != null)
        {
            objectsInTrigger.Add(rb);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;

        if (rb != null)
        {
            objectsInTrigger.Remove(rb);
        }
    }

    private void Update()
    {
        bool isPressed = HasObjects;

        if (isPressed && !wasPressed)
        {
            powerActivator.Activate();
        }
        else if (!isPressed && wasPressed)
        {
            powerActivator.Deactivate();
        }

        wasPressed = isPressed;
    }
}