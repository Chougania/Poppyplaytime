using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

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

        EnsureLobbyUI();
        EnsureLobbyRoom();

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

        if (playerPreview == null && NetworkManager.Singleton != null &&
            NetworkManager.Singleton.NetworkConfig.PlayerPrefab != null)
        {
            GameObject preview = Object.Instantiate(NetworkManager.Singleton.NetworkConfig.PlayerPrefab);
            preview.name = "PoppyMenu_PlayerPreview";
            NetworkBehaviour[] networkBehaviours = preview.GetComponentsInChildren<NetworkBehaviour>(true);
            for (int i = 0; i < networkBehaviours.Length; i++)
                networkBehaviours[i].enabled = false;

            Camera[] previewCameras = preview.GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < previewCameras.Length; i++)
                previewCameras[i].enabled = false;

            AudioListener[] listeners = preview.GetComponentsInChildren<AudioListener>(true);
            for (int i = 0; i < listeners.Length; i++)
                listeners[i].enabled = false;

            Collider[] colliders = preview.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;

            CharacterController[] controllers = preview.GetComponentsInChildren<CharacterController>(true);
            for (int i = 0; i < controllers.Length; i++)
                controllers[i].enabled = false;

            Rigidbody[] bodies = preview.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < bodies.Length; i++)
            {
                bodies[i].isKinematic = true;
                bodies[i].useGravity = false;
            }

            playerPreview = preview.transform;
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

    private static void EnsureLobbyRoom()
    {
        if (Object.FindObjectOfType<PoppyLobbyRoomBuilder>() != null)
            return;

        GameObject roomRoot = new GameObject("PoppyLobbyRoom_Runtime");
        PoppyLobbyRoomBuilder room = roomRoot.AddComponent<PoppyLobbyRoomBuilder>();
        GameManager manager = Object.FindObjectOfType<GameManager>();
        if (manager != null)
            room.anchor = manager.lobbySpawnPoint;
    }

    private static void EnsureLobbyUI()
    {
        if (Object.FindObjectOfType<PoppyLobbyUI>() != null)
            return;

        GameObject lobbyRoot = new GameObject("PoppyLobbyUI_Runtime");
        PoppyLobbyUI lobby = lobbyRoot.AddComponent<PoppyLobbyUI>();
        lobby.connectionManager = Object.FindObjectOfType<ConnectionManafer>();
        lobby.gameManager = Object.FindObjectOfType<GameManager>();
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
