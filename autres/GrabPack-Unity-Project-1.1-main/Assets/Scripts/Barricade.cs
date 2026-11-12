using System.Collections.Generic;
using UnityEngine;

public class Barricade : MonoBehaviour
{
    private Rigidbody rb;

    public float pullFactor = 1f;
    public bool isgrabbingRight;
    public bool Pulled = false;
    public float rotationSpeed = 1f;

    private List<LaunchHand> hands = new List<LaunchHand>();

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;

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
            if (!child.name.StartsWith("Hand"))
                continue;

            if (child.TryGetComponent(out LaunchHand hand))
                hands.Add(hand);
        }
    }

    void Update()
    {
        if (pullFactor < 0 && !Pulled)
        {
            rb.isKinematic = false;
            Pulled = true;
        }
    }

    void FixedUpdate()
    {
        bool rightHandFound = false;

        foreach (LaunchHand hand in hands)
        {
            if (!hand.IsHeld())
                continue;

            bool rightHand = hand.Hand == "Right";
            bool leftHand = hand.Hand == "Left";

            if (rightHand)
            {
                PullBarricade();
                isgrabbingRight = true;
                rightHandFound = true;
            }

            if (leftHand && !isgrabbingRight)
            {
                PullBarricade();
            }
        }

        if (!rightHandFound)
            isgrabbingRight = false;
    }

    void PullBarricade()
    {
        pullFactor -= Time.deltaTime;

        if (!Pulled)
        {
            transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
        }
    }
}