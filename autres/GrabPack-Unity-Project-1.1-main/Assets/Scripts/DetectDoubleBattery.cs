using UnityEngine;

public class DetectDoubleBattery : MonoBehaviour
{
    public GameObject door1;
    public GameObject door2;
    public GameObject door3;

    public bool powered = false;

    public GameObject[] objToDisable;
    public GameObject[] objToEnable;

    public Animator anim;

    void Update()
    {
        if (!powered)
        {
            if (!door1.activeInHierarchy && !door2.activeInHierarchy && !door3.activeInHierarchy)
            {
                powered = true;
                Power();
            }
        }

    }

    public void Power()
    {
        anim.SetBool("open", true);

        foreach (GameObject obj in objToDisable)
        {
            obj.SetActive(false);
        }

        foreach (GameObject obj in objToEnable)
        {
            obj.SetActive(true);
        }

    }
}
