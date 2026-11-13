using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interact : MonoBehaviour
{
    public float interactionRange = 2f;

    public LayerMask buttonLayer;
    public LayerMask CodeCheckerLayer;
    public LayerMask grabpackinteractable;
    public LayerMask handpickups;
    public LayerMask EMUPickup;
    public LayerMask keycard;
    public LayerMask glowbyPickup;

    public Animator grabpack;
    public GameObject interactButtonUI;

    private LayerMask interactLayers;
    private bool canInteract;

    public MobileIcons mobileIcons;


    void Start()
    {
        interactLayers = buttonLayer | CodeCheckerLayer | grabpackinteractable | handpickups | EMUPickup | keycard | glowbyPickup;
    }

    void Update()
    {
        CheckForInteractable();

        if (canInteract && Input.GetKeyDown(KeyCode.E))
        {
            RaycastInteraction();
        }
    }

    void CheckForInteractable()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        canInteract = Physics.Raycast(ray, interactionRange, interactLayers);

        if (mobileIcons.isMobile)
        {
            if (interactButtonUI != null)
                interactButtonUI.SetActive(canInteract);
        }

    }

    public void RaycastInteraction()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionRange, interactLayers))
        {
            GameObject hitObject = hit.transform.gameObject;

            if (((1 << hitObject.layer) & buttonLayer) != 0)
            {
                keyButton keyButton = hitObject.GetComponent<keyButton>();
                if (keyButton != null)
                    keyButton.pushed();
            }
            else if (((1 << hitObject.layer) & CodeCheckerLayer) != 0)
            {
                CheckCode checkCode = hitObject.GetComponent<CheckCode>();
                if (checkCode != null)
                    checkCode.Check();
            }
            else if (((1 << hitObject.layer) & grabpackinteractable) != 0)
            {
                PickupGrabpack pickup = hitObject.GetComponent<PickupGrabpack>();
                if (pickup != null)
                {
                    pickup.Pickup();
                    if (grabpack != null)
                        grabpack.SetTrigger("pickup");
                }
            }
            else if (((1 << hitObject.layer) & handpickups) != 0)
            {
                HandPickUp pickup = hitObject.GetComponent<HandPickUp>();
                if (pickup != null)
                    pickup.PickupHand();
            }
            else if (((1 << hitObject.layer) & EMUPickup) != 0)
            {
                EMUPickup pickup = hitObject.GetComponent<EMUPickup>();
                if (pickup != null)
                    pickup.Pickup();
            }
            else if (((1 << hitObject.layer) & keycard) != 0)
            {
                KeyCardScanner pickup = hitObject.GetComponent<KeyCardScanner>();
                if (pickup != null)
                    pickup.Insert();
            }
            else if (((1 << hitObject.layer) & glowbyPickup) != 0)
            {
                GlowbyPickup pickup = hitObject.GetComponent<GlowbyPickup>();
                if (pickup != null)
                    pickup.Pickup();
            }
        }
    }
}