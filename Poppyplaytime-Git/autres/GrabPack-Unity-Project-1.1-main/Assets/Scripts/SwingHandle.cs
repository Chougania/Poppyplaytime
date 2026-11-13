using System.Collections.Generic;
using UnityEngine;

public class SwingHandle : MonoBehaviour
{
    public Rigidbody Player;
    public float forceStrength = 10f;
    public float yForceMultiplier = 0.5f;

    public bool isgrabbingRight;
    public bool grabbed = false;

    public AudioSource globalaudio;
    public AudioClip swingsfx;

    private bool virtualHeld;

    private List<LaunchHand> hands = new List<LaunchHand>();

    bool isMobile = false;

    public void UIButtonDown()
    {
        virtualHeld = true;
    }

    public void UIButtonUp()
    {
        virtualHeld = false;
    }

    void Start()
    {
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

    void FixedUpdate()
    {
        bool rightHandFound = false;
        bool anyHandFound = false;

        foreach (LaunchHand hand in hands)
        {
            if (!hand.IsHeld())
                continue;

            anyHandFound = true;

            bool rightHand = hand.Hand == "Right";
            bool leftHand = hand.Hand == "Left";

            if (rightHand)
            {
                ApplySwingForce();
                isgrabbingRight = true;
                rightHandFound = true;
            }

            if (leftHand && !isgrabbingRight)
            {
                ApplySwingForce();
            }
        }

        if (!rightHandFound)
            isgrabbingRight = false;

        if (!anyHandFound)
            grabbed = false;
    }

    void ApplySwingForce()
    {
        Vector3 direction = (transform.position - Player.position).normalized;
        direction.y *= yForceMultiplier;

        Player.AddForce(direction * forceStrength, ForceMode.Force);

        if (!grabbed)
        {
            globalaudio.PlayOneShot(swingsfx, 1f);
            grabbed = true;
        }
    }
}