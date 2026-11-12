using System.Collections.Generic;
using UnityEngine;

public class KeyCard : MonoBehaviour
{
    private MeshRenderer renderer;
    private BoxCollider collider;

    private List<LaunchHand> hands = new List<LaunchHand>();

    public bool PICKED = false;

    void Start()
    {
        renderer = GetComponent<MeshRenderer>();
        collider = GetComponent<BoxCollider>();

        RefreshHands();
    }

    void OnTransformChildrenChanged()
    {
        RefreshHands();
    }

    void RefreshHands()
    {
        hands.Clear();

        foreach (Transform child in transform)
        {
            if (child.TryGetComponent(out LaunchHand hand))
                hands.Add(hand);
        }
    }

    void Update()
    {
        foreach (LaunchHand hand in hands)
        {
            PickUp(hand);
            break;
        }
    }

    void PickUp(LaunchHand triggeringHand)
    {
        renderer.enabled = false;
        collider.enabled = false;
        PICKED = true;


        triggeringHand.return1();
    }
}