using UnityEngine;
using UnityEngine.SceneManagement;


public class AutoSaveTrigger : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Vector3 pos = other.transform.position;

            PlayerPrefs.SetString("SavedScene", SceneManager.GetActiveScene().name);

            PlayerPrefs.SetFloat("PlayerX", pos.x);
            PlayerPrefs.SetFloat("PlayerZ", pos.z);
            PlayerPrefs.SetFloat("PlayerY", pos.y);

            PlayerPrefs.Save();


        }
    }

}