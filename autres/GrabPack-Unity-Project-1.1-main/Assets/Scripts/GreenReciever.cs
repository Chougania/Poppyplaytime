using UnityEngine;

[RequireComponent(typeof(PowerActivator))]
[RequireComponent(typeof(AudioSource))]
public class GreenReciever : MonoBehaviour
{
    private PowerActivator powerActivator;
    private AudioSource audioSource;

    [SerializeField] private AudioClip greenSFX;

    [SerializeField] private ParticleSystem grabParticles;
    [SerializeField] private GameObject poweredLight;

    public float lifeTime = 15f;

    private float liveCount;
    private bool powered;
    private bool played;

    void Awake()
    {
        powerActivator = GetComponent<PowerActivator>();
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        liveCount = lifeTime;
    }

    void Update()
    {
        Transform hand = transform.Find("Hand_Conductive");
        Transform hand2 = transform.Find("Hand_Green");

        if (hand != null)
        {
            Conductor conductor = hand.GetComponent<Conductor>();
            LaunchHand launchHand = hand.GetComponent<LaunchHand>();

            if (!played && conductor != null && conductor.CurrentElement == "green")
            {
                ActivatePower();

                if (grabParticles != null)
                    grabParticles.Play();

                if (greenSFX != null)
                    audioSource.PlayOneShot(greenSFX, 3f);

                if (launchHand != null)
                    launchHand.return1();

                played = true;
            }
        }
        else if (hand2 != null)
        {
            GreenHand greenhand = hand2.GetComponent<GreenHand>();
            LaunchHand launchHand = hand2.GetComponent<LaunchHand>();

            if (!played && greenhand != null && greenhand.isPowered == true)
            {
                ActivatePower();

                if (grabParticles != null)
                    grabParticles.Play();

                if (greenSFX != null)
                    audioSource.PlayOneShot(greenSFX, 3f);

                if (launchHand != null)
                    launchHand.return1();

                greenhand.Deactivate();

                played = true;
            }
        }
        else
        {
            played = false;
        }


        if (powered)
        {
            liveCount -= Time.deltaTime;

            if (liveCount <= 0)
                DeactivatePower();
        }
    }

    void ActivatePower()
    {
        powered = true;
        liveCount = lifeTime;

        if (poweredLight != null)
            poweredLight.SetActive(true);

        powerActivator.Activate();
    }

    void DeactivatePower()
    {
        powered = false;
        liveCount = lifeTime;

        if (poweredLight != null)
            poweredLight.SetActive(false);

        powerActivator.Deactivate();
    }
}