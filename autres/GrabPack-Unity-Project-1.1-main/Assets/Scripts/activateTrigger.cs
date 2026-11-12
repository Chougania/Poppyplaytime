using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class activateTrigger : MonoBehaviour
{
    public GameObject[] objectsToActivate;
    private bool triggered = false;

    public bool disableafteruse = true;

    public string TagName = "Player";

    void Start()
    {
        foreach (GameObject obj in objectsToActivate)
        {
            obj.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        if (other.CompareTag(TagName))
        {
            triggered = true;

            foreach (GameObject obj in objectsToActivate)
            {

                obj.SetActive(true);
            }

            if (disableafteruse)
            {
                StartCoroutine(DisableNextFrame());

            }
        }
    }

    IEnumerator DisableNextFrame()
    {
        yield return null;



        gameObject.SetActive(false);
    }
}