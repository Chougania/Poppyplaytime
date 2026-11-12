using UnityEngine;

public class FlareBehavior : MonoBehaviour
{
    private bool hasCollided = false;
    private Rigidbody rb;

    private bool used = false;

    public AudioClip scaredNoise;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        Destroy(gameObject, 30f);

        used = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasCollided) return;

        if (collision.gameObject.isStatic)
        {
            hasCollided = true;

            if (rb != null)
            {
                rb.isKinematic = true;
            }
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        if (!used)
        {
            if (other.CompareTag("critter"))
            {
                SmilingCritterAI critterai = other.gameObject.GetComponent<SmilingCritterAI>();
                AudioSource audioSource = other.gameObject.GetComponent<AudioSource>();

                if (critterai != null)
                {
                    if (critterai.currentState != SmilingCritterAI.State.Friendly)
                    {
                        critterai.currentState = SmilingCritterAI.State.Return;
                        used = true;
                        audioSource.PlayOneShot(scaredNoise, 1.0f);
                    }



                }
            }
        }

    }
}