using System.Collections.Generic;
using UnityEngine;

public class MagneticPull : MonoBehaviour
{
    public RigidboyPlayerController controller;
    public Rigidbody Player;
    public float pullSpeed = 20f;

    private bool wasBeingPulled = false;
    public bool isgrabbingRight;
    public bool grabbed = false;
    public bool isBeingPulled = false;

    public AudioSource globalaudio;
    public AudioClip pullsfx;

    public HandManager handmanager;
    public GameObject dragaudio;

    public magnetHand MagnetHand;
    private bool magnetWasHeld = false;
    private List<LaunchHand> hands = new List<LaunchHand>();

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
            if (!child.name.StartsWith("Hand"))
                continue;

            if (child.TryGetComponent(out LaunchHand hand))
                hands.Add(hand);
        }
    }

    void FixedUpdate()
    {
        bool hasCuffs = handmanager.HasEMUCuffs;

        bool rightHandFound = false;

        isBeingPulled = false;

        foreach (LaunchHand hand in hands)
        {
            string name = hand.gameObject.name;

            if (name == "Hand_Magnet")
            {
                bool isHeld = hand.IsHeld();

                if (isHeld && !magnetWasHeld)
                {
                    MagnetHand.TogglePolarity();
                    hand.return1();
                }

                magnetWasHeld = isHeld;
            }

            if (!hand.IsHeld())
                continue;

            bool rightHand =
                name == "Hand_Rocket" ||
                name == "Hand_Red" ||
                name == "Hand_Pressure" ||
                name == "Hand_Magnet" ||
                name == "Hand_Green" ||
                name == "Hand_Conductive";

            bool leftHand = name == "Hand_Blue";

            if (rightHand)
            {
                if (name != "Hand_Magnet" && hasCuffs)
                {
                    ApplyPull();
                    isgrabbingRight = true;
                    rightHandFound = true;
                    isBeingPulled = true;
                }
            }

            if (leftHand && !isgrabbingRight && hasCuffs)
            {
                ApplyPull();
                isBeingPulled = true;
            }
        }

        if (!rightHandFound)
            isgrabbingRight = false;

        HandlePullState();
    }

    void HandlePullState()
    {
        if (isBeingPulled && !wasBeingPulled)
        {
            controller.CanMove = false;
        }

        if (!isBeingPulled && wasBeingPulled)
        {
            controller.CanMove = true;
            grabbed = false;
            Player.useGravity = true;
            globalaudio.Stop();
            dragaudio.SetActive(false);


        }

        wasBeingPulled = isBeingPulled;
    }

    void ApplyPull()
    {
        Player.useGravity = false;
        dragaudio.SetActive(true);

        Vector3 newPosition = Vector3.MoveTowards(
            Player.position,
            transform.position,
            pullSpeed * Time.fixedDeltaTime
        );

        Player.MovePosition(newPosition);

        if (!grabbed)
        {
            globalaudio.PlayOneShot(pullsfx, 0.3f);
            grabbed = true;
        }
    }

    public void RejectHand()
    {
        LaunchHand[] hands = GetComponentsInChildren<LaunchHand>();

        foreach (LaunchHand hand in hands)
        {
            hand.return1();
        }
    }
}