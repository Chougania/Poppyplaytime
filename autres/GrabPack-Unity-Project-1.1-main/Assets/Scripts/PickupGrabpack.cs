using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupGrabpack : MonoBehaviour
{
    public bool hasRedHand = true;
    public bool hasBlueHand = true;
    public bool hasPurpleHand = true;
    public bool hasPressureHand = true;
    public bool hasConductiveHand = true;
    public bool hasMagnetHand = true;
    public bool hasFireHand = true;
    public bool hasGreenHand = true;


    public GameObject RedHand;
    public GameObject PurpleHand;
    public GameObject FlareHand;
    public GameObject conductiveHand;
    public GameObject BlueHand;
    public GameObject MagnetHand;
    public GameObject FireHand;
    public GameObject GreenHand;


    public GameObject MockRedHand;
    public GameObject MockPurpleHand;
    public GameObject MockFlareHand;
    public GameObject MockconductiveHand;
    public GameObject MockBlueHand;
    public GameObject MockMagnetHand;
    public GameObject MockFireHand;
    public GameObject MockGreenHand;

    public HandManager handmanager;

    public RigidboyPlayerController player;
    public MobileIcons mobileicons;

    public SkinnedMeshRenderer rendererToActivate;
    public SkinnedMeshRenderer rendererToDeactivate;
    public SkinnedMeshRenderer ExtraArmRenderer;
    public bool activateExtraArm = false;
    public string grabpackversion;

    public GameObject otherGrabpack;

    public AudioSource audiosource;
    public AudioClip pickupSFX;

    public Material ArmMaterial;

    public void Pickup()
    {
        gameObject.SetActive(false);
        audiosource.PlayOneShot(pickupSFX, 1.0f);
        BlueHand.SetActive(hasBlueHand);

        RedHand.SetActive(false);
        PurpleHand.SetActive(false);
        FlareHand.SetActive(false);
        conductiveHand.SetActive(false);
        MagnetHand.SetActive(false);
        FireHand.SetActive(false);
        GreenHand.SetActive(false);

        if (hasRedHand)
            RedHand.SetActive(true);
        else if (hasPurpleHand)
            PurpleHand.SetActive(true);
        else if (hasPressureHand)
            FlareHand.SetActive(true);
        else if (hasConductiveHand)
            conductiveHand.SetActive(true);
        else if (hasMagnetHand)
            MagnetHand.SetActive(true);
        else if (hasFireHand)
            FireHand.SetActive(true);
        else if (hasGreenHand)
            GreenHand.SetActive(true);

        handmanager.hasGrabPack = true;


        rendererToActivate.enabled = true;
        rendererToDeactivate.enabled = false;
        ExtraArmRenderer.material = ArmMaterial;

        if (grabpackversion != handmanager.grabpackversion)
        {
            if (handmanager.grabpackversion != "none")
            {
                otherGrabpack.transform.position = gameObject.transform.position;
                otherGrabpack.SetActive(true);

                PickupGrabpack pickupgrabpack = otherGrabpack.GetComponent<PickupGrabpack>();

                pickupgrabpack.hasRedHand = handmanager.hasRedHand;
                pickupgrabpack.hasBlueHand = handmanager.hasBlueHand;
                pickupgrabpack.hasPressureHand = handmanager.hasPressureHand;
                pickupgrabpack.hasPurpleHand = handmanager.hasPurpleHand;
                pickupgrabpack.hasConductiveHand = handmanager.hasConductiveHand;
                pickupgrabpack.hasMagnetHand = handmanager.hasMagnetHand;
                pickupgrabpack.hasFireHand = handmanager.hasFireHand;
                pickupgrabpack.hasGreenHand = handmanager.hasGreenHand;

            }

        }
        handmanager.hasRedHand = hasRedHand;
        handmanager.hasBlueHand = hasBlueHand;
        handmanager.hasPressureHand = hasPressureHand;
        handmanager.hasPurpleHand = hasPurpleHand;
        handmanager.hasConductiveHand = hasConductiveHand;
        handmanager.hasMagnetHand = hasMagnetHand;
        handmanager.hasFireHand = hasFireHand;
        handmanager.hasGreenHand = hasGreenHand;

        handmanager.grabpackversion = grabpackversion;

        player.UpdateHandButtons();

        if (mobileicons.isMobile)
        {
            mobileicons.UpdateMobileIcons();

        }
    }

    void Start()
    {
        if (handmanager.hasGrabPack == true)
        {
            gameObject.SetActive(false);
        }
        rendererToActivate.enabled = false;
        rendererToDeactivate.enabled = false;
        //ExtraArm.SetActive(false);

        if (hasBlueHand)
        {
            MockBlueHand.SetActive(true);
        }


        if (hasRedHand)
            MockRedHand.SetActive(true);
        else if (hasPurpleHand)
            MockPurpleHand.SetActive(true);
        else if (hasPressureHand)
            MockFlareHand.SetActive(true);
        else if (hasConductiveHand)
            MockconductiveHand.SetActive(true);
        else if (hasMagnetHand)
            MockMagnetHand.SetActive(true);
        else if (hasFireHand)
            MockFireHand.SetActive(true);
        else if (hasGreenHand)
            MockGreenHand.SetActive(true);
    }
}
