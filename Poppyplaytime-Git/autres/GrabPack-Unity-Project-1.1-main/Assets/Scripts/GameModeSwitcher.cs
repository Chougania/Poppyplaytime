using UnityEngine;

public class GameModeSwitcher : MonoBehaviour
{
    public GameObject gameplayController;
    public GameObject editorController;

    public Transform spawnPoint;

    public KeyCode toggleKey = KeyCode.F1;

    bool inEditor = false;

    void Start()
    {
        SetMode(true);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            inEditor = !inEditor;
            SetMode(inEditor);
        }
    }

    void SetMode(bool editorMode)
    {
        editorController.SetActive(editorMode);
        gameplayController.SetActive(!editorMode);

        if (!editorMode && spawnPoint != null)
        {
            gameplayController.transform.position = spawnPoint.position;
            gameplayController.transform.rotation = spawnPoint.rotation;
        }

        Cursor.lockState = editorMode ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = editorMode;
    }
}