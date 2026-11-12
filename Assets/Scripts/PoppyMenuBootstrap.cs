using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Automatically installs the runtime menu on the Lobby scene so no manual scene YAML
/// surgery is needed. Assignments are resolved from existing scene objects by name/tag.
/// </summary>
public static class PoppyMenuBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryInstall(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryInstall(scene);
    }

    private static void TryInstall(Scene scene)
    {
        if (!scene.IsValid() || scene.name != "Lobby")
            return;

        if (Object.FindObjectOfType<PoppyMainMenu>() != null)
            return;

        ConnectionManafer connection = Object.FindObjectOfType<ConnectionManafer>();
        Camera menuCamera = Camera.main;
        if (menuCamera == null)
        {
            Camera[] cameras = Object.FindObjectsOfType<Camera>();
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null && cameras[i].enabled)
                {
                    menuCamera = cameras[i];
                    break;
                }
            }
        }

        Transform playerPreview = null;
        try
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerPreview = player.transform;
        }
        catch (UnityException)
        {
            // The project may not define a Player tag in a custom tag setup.
        }

        Transform huggyPreview = FindHuggyPreview();
        GameObject menuRoot = new GameObject("PoppyMainMenu_Runtime");
        PoppyMainMenu menu = menuRoot.AddComponent<PoppyMainMenu>();
        menu.menuCamera = menuCamera;
        menu.connectionManager = connection;
        menu.characterPreview = playerPreview;
        menu.huggyPreview = huggyPreview;
        menu.buildCorridorAtRuntime = true;
    }

    private static Transform FindHuggyPreview()
    {
        Transform[] all = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < all.Length; i++)
        {
            Transform candidate = all[i];
            if (candidate == null || !candidate.gameObject.scene.IsValid())
                continue;

            if (candidate.name == "Poppy_Playtime__Box_-_Huggy_Wuggy")
                return candidate;
        }

        return null;
    }
}
