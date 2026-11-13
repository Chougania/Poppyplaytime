using UnityEngine;
using System.Collections.Generic;

public class ElectricalReciever : MonoBehaviour
{
    [SerializeField] private List<PowerPole> polesInPuzzle;


    private PowerActivator powerActivator;

    private bool complete;

    void Awake()
    {
        powerActivator = GetComponent<PowerActivator>();
    }

    void Update()
    {
        if (complete) return;

        if (AllPolesPowered() && HasHandAttached())
        {
            CompleteCircuit();
        }
    }

    private bool AllPolesPowered()
    {
        foreach (PowerPole pole in polesInPuzzle)
        {
            if (!pole.powered)
                return false;
        }
        return true;
    }

    private bool HasHandAttached()
    {
        foreach (Transform child in transform)
        {
            if (child.GetComponent<LaunchHand>() != null)
                return true;
        }

        return false;
    }

    private void CompleteCircuit()
    {
        complete = true;

        if (powerActivator != null)
            powerActivator.Activate();


        ReturnAllHands();
    }

    private void ReturnAllHands()
    {
        LaunchHand[] hands = FindObjectsOfType<LaunchHand>();

        foreach (LaunchHand hand in hands)
        {
            hand.return1();
        }
    }
}