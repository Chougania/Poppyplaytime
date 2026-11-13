using UnityEngine;
using UnityEngine.SceneManagement;


public class AutoSaveScene : MonoBehaviour
{
    public const string SavedSceneKey = "SavedScene";

    public string[] saveableScenes;

    public int DefaultScene = 1;


    private void Awake()
    {
        //DontDestroyOnLoad(gameObject);

        //if (FindObjectsOfType(GetType()).Length > 1)
        //{
           // Destroy(gameObject);
       // }
    }

    void Start()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        foreach (string scene in saveableScenes)
        {
            if (scene == currentScene)
            {
                PlayerPrefs.SetString(SavedSceneKey, currentScene);
                PlayerPrefs.Save();

                break;
            }
        }
    }

    public void LoadSavedScene()
    {
        if (PlayerPrefs.HasKey(SavedSceneKey))
        {
            SceneManager.LoadScene(PlayerPrefs.GetString(SavedSceneKey));
        }
        else
        {
            SceneManager.LoadScene(DefaultScene);
        }

    }


    public void ClearSave()
    {
        PlayerPrefs.DeleteKey(SavedSceneKey);
    }

}
