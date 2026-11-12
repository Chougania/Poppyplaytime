using UnityEngine;

public class freeze : MonoBehaviour
{
    public AudioSource globalAudio;
    public AudioClip icesfx;
    public AudioClip firesfx;

    private bool played = false;

    public ParticleSystem grabparticles;
    public ParticleSystem heatedparticles;

    public MeshRenderer[] renderers;

    public Material frozenmat;
    public Material unfrozen;

    public bool frozen = false;

    public Breakable breakable;

    private Conductor conductor;
    private LaunchHand launchhand;

    void OnTransformChildrenChanged()
    {
        FindConductiveHand();
    }

    void Start()
    {
        FindConductiveHand();
    }

    void FindConductiveHand()
    {
        conductor = null;
        launchhand = null;

        foreach (Transform child in transform)
        {
            if (child.name == "Hand_Conductive")
            {
                child.TryGetComponent(out conductor);
                child.TryGetComponent(out launchhand);
                break;
            }
        }
    }

    void Update()
    {
        if (conductor == null)
        {
            played = false;
            return;
        }

        if (!played && conductor.CurrentElement == "ice" && !frozen)
        {
            FreezeObject();
        }

        if (!played && conductor.CurrentElement == "fire" && frozen)
        {
            UnfreezeObject();
        }
    }

    void FreezeObject()
    {
        if (grabparticles != null)
            grabparticles.Play();

        foreach (MeshRenderer r in renderers)
            r.sharedMaterial = frozenmat;

        globalAudio.PlayOneShot(icesfx, 3f);

        played = true;
        frozen = true;

        breakable.isbreakable = true;

        if (launchhand != null)
            launchhand.return1();
    }

    void UnfreezeObject()
    {
        if (heatedparticles != null)
            heatedparticles.Play();

        foreach (MeshRenderer r in renderers)
            r.sharedMaterial = unfrozen;

        globalAudio.PlayOneShot(firesfx, 3f);

        played = true;
        frozen = false;

        breakable.isbreakable = false;

        if (launchhand != null)
            launchhand.return1();
    }
}