using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Door : MonoBehaviour
{
    public bool Locked = false;
    public Animator animator;

    public AudioClip openSFX;
    public AudioClip closeSFX;
    public AudioClip lockedSFX;

    private AudioSource audioSource;

    private bool handTriggered; 

    public bool isOpen = false;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

    }

    void Start()
    {
        if (isOpen && !Locked)
        {
            ToggleDoor();
        }
    }

    void Update()
    {


        HandleKeyboard();
        HandleHandInteraction();
    }

    void HandleKeyboard()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Camera cam = Camera.main;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 2f))
            {
                if (hit.collider.GetComponent<Door>() == this)
                {
                    ToggleDoor();
                }
            }
        }
    }

    void HandleHandInteraction()
    {
        bool handAttached = false;

        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Hand"))
            {
                handAttached = true;
                break;
            }
        }

        if (handAttached && !handTriggered)
        {
            ToggleDoor();
            handTriggered = true;
        }

        if (!handAttached)
        {
            handTriggered = false;
        }
    }

    public void ToggleDoor()
    {
        if (!Locked)
        {

            bool open = animator.GetBool("open");

            animator.SetBool("open", !open);

            if (open)
            {
                audioSource.PlayOneShot(closeSFX);
                isOpen = false;
            }
            else
            {
                audioSource.PlayOneShot(openSFX);
                isOpen = true;
            }
        }
        if (Locked)
        {
            animator.SetTrigger("locked");
            audioSource.PlayOneShot(lockedSFX);

        }
        NoiseEmitter.EmitNoise(transform.position, 0.5f, transform);


    }

    void OnDisable()
    {
        Debug.Log("Door disabled!", this);
        Debug.Log(System.Environment.StackTrace);
    }

    public void Unlock()
    {
        Locked = false;
    }

    public void Lock()
    {
        Locked = true;
    }
}