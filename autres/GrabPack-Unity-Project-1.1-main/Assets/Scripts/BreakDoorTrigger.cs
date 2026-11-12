using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BreakDoorTrigger : MonoBehaviour
{

    public ParticleSystem SmokeParticles;
    public Door door;

    public Rigidbody rb;
    public Animator anim;

    public AudioSource audiosource;
    public AudioClip breakSFX;

    public bool broken = false;

    public void OnTriggerEnter(Collider other)
    {
        if (other.tag == "AI")
        {
            if (!door.isOpen)
            {
                BreakDoor();
            }
        }
    }

    void BreakDoor()
    {
        if (!broken)
        {

            broken = true;
            rb.isKinematic = false;
            door.enabled = false;
            anim.enabled = false;
            SmokeParticles.Play();
            audiosource.PlayOneShot(breakSFX, 3.0f);
        }

    }

}
