using UnityEngine;

public class HandManager : MonoBehaviour
{
    public bool hasRedHand = true;
    public bool hasBlueHand = true;
    public bool hasPurpleHand = true;
    public bool hasPressureHand = true;
    public bool hasConductiveHand = true;
    public bool HasEMUCuffs = false;
    public bool hasMagnetHand = true;
    public bool hasFireHand = true;
    public bool hasGreenHand = true;

    public bool hasGrabPack = true;
    public GameObject grabpack;

    private bool lastGrabPackState;

    public string grabpackversion = "none";

    void Start()
    {
        if (!hasRedHand && !hasBlueHand && !hasPurpleHand && !hasPressureHand && !hasConductiveHand && !hasMagnetHand && !hasFireHand && !hasGreenHand)
        {
            hasGrabPack = false;
        }


        if (!hasGrabPack)
        {
            hasRedHand = false;
            hasBlueHand = false;
            hasPurpleHand = false;
            hasPressureHand = false;
            hasConductiveHand = false;
            hasMagnetHand = false;
            hasFireHand = false;
            hasGreenHand = false;

        }


        grabpack.SetActive(hasGrabPack);
        lastGrabPackState = hasGrabPack;
    }

    void Update()
    {

        if (hasGrabPack != lastGrabPackState)
        {
            grabpack.SetActive(hasGrabPack);
            lastGrabPackState = hasGrabPack;
        }
    }
}