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
        GameObject previewObject = null;
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.NetworkConfig.PlayerPrefab != null)
        {
            previewObject = Object.Instantiate(NetworkManager.Singleton.NetworkConfig.PlayerPrefab);
            previewObject.name = "PoppyMenu_PlayerPreview";
            previewObject.transform.position = Vector3.zero;

            // A menu preview is a visual-only clone. Disable all gameplay scripts and
            // non-visual behaviours so it cannot move, shoot the GrabPack or join Netcode.
            Behaviour[] previewBehaviours = previewObject.GetComponentsInChildren<Behaviour>(true);
            for (int i = 0; i < previewBehaviours.Length; i++)
            {
                if (!(previewBehaviours[i] is Animator))
                    previewBehaviours[i].enabled = false;
            }

            NetworkObject networkObject = previewObject.GetComponent<NetworkObject>();
            if (networkObject != null && networkObject.IsSpawned)
                networkObject.Despawn(true);

            Camera[] previewCameras = previewObject.GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < previewCameras.Length; i++)
                previewCameras[i].enabled = false;

            AudioListener[] listeners = previewObject.GetComponentsInChildren<AudioListener>(true);
            for (int i = 0; i < listeners.Length; i++)
                listeners[i].enabled = false;

            Collider[] colliders = previewObject.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;

            CharacterController[] controllers = previewObject.GetComponentsInChildren<CharacterController>(true);
            for (int i = 0; i < controllers.Length; i++)
                controllers[i].enabled = false;

            Rigidbody[] bodies = previewObject.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < bodies.Length; i++)
            {
                bodies[i].isKinematic = true;
                bodies[i].useGravity = false;
                bodies[i].velocity = Vector3.zero;
                bodies[i].angularVelocity = Vector3.zero;
            }

            Animator[] animators = previewObject.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                animators[i].speed = 0f;
                animators[i].applyRootMotion = false;
            }

            playerPreview = previewObject.transform;
        }
        else
        {
            Debug.LogWarning("PoppyMenuBootstrap: NetworkManager PlayerPrefab is missing; player showroom will be empty.");
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
