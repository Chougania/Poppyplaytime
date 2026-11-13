using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class limon : MonoBehaviour
{

    public AudioClip eatnoise;
    public GameObject particlesPrefab;

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("critter"))
        {
            SmilingCritterAI critterai = other.gameObject.GetComponent<SmilingCritterAI>();
            AudioSource audioSource = other.gameObject.GetComponent<AudioSource>();

            if (critterai != null)
            {
                critterai.BecomeFriendly();
                audioSource.PlayOneShot(eatnoise, 1.0f);

                if (particlesPrefab != null)
                {
                    Vector3 spawnPos = other.transform.position + Vector3.up * 0.5f;

                    GameObject spawned = Instantiate(
                        particlesPrefab,
                        spawnPos,
                        Quaternion.identity
                    );

                    spawned.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

                    spawned.transform.localScale = Vector3.one;
                }

                Destroy(gameObject);


            }
        }

    }
}