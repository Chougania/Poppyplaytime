using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadPlayerPosition : MonoBehaviour
{
    void Start()
    {

        if (!PlayerPrefs.HasKey("SavedScene"))
            return;


        if (!PlayerPrefs.HasKey("PlayerX"))
            return;

        string savedScene = PlayerPrefs.GetString("SavedScene");

        if (SceneManager.GetActiveScene().name != savedScene)
            return;

        float x = PlayerPrefs.GetFloat("PlayerX");
        float z = PlayerPrefs.GetFloat("PlayerZ");
        float y = PlayerPrefs.GetFloat("PlayerY");

        transform.position = new Vector3(x, y, z);

    }

}
