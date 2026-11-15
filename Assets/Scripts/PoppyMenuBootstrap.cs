using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

/// <summary>
/// Installs the menu and waiting-room UI only. The developer builds all 3D scenes in Unity.
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

            SanitizePreview(previewObject);

            playerPreview = previewObject.transform;
        }
        else
        {
            Debug.LogWarning("PoppyMenuBootstrap: NetworkManager PlayerPrefab is missing; player showroom will be empty.");
        }

        Transform huggyPreview = CreateRealHuggyPreview();
        if (huggyPreview != null)
            HideBoxProp();
        else
            huggyPreview = FindHuggyPreview(); // repli : l'ancienne boîte si le Player Prefab est introuvable
        GameObject menuRoot = new GameObject("PoppyMainMenu_Runtime");
        PoppyMainMenu menu = menuRoot.AddComponent<PoppyMainMenu>();
        menu.menuCamera = menuCamera;
        menu.connectionManager = connection;
        menu.characterPreview = playerPreview;
        menu.huggyPreview = huggyPreview;
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

    // Aperçu purement visuel : aucun script de jeu, aucun réseau, aucune physique.
    private static void SanitizePreview(GameObject previewObject)
    {
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
    }

    // Vrai modèle de Huggy Wuggy (mesh SK_Huggy_New du Player Prefab), et non la boîte à jouet de la scène.
    private static Transform CreateRealHuggyPreview()
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null || nm.NetworkConfig == null || nm.NetworkConfig.PlayerPrefab == null)
            return null;

        GameObject prefab = nm.NetworkConfig.PlayerPrefab;
        DbUnityMovment source = prefab.GetComponent<DbUnityMovment>();
        if (source == null || source.monsterModelContainer == null)
            return null;

        GameObject clone = Object.Instantiate(prefab);
        clone.name = "PoppyMenu_HuggyPreview";
        clone.transform.position = Vector3.zero;
        SanitizePreview(clone);

        DbUnityMovment mv = clone.GetComponent<DbUnityMovment>();
        if (mv == null || mv.monsterModelContainer == null)
        {
            Object.Destroy(clone);
            return null;
        }
        if (mv.survivorModelContainer != null) mv.survivorModelContainer.SetActive(false);
        if (mv.deadModelContainer != null) mv.deadModelContainer.SetActive(false);
        mv.monsterModelContainer.SetActive(true);

        // éléments de gameplay qui ne doivent pas apparaître dans le menu
        foreach (Transform t in clone.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name;
            if (n == "player hitbox" || n == "Jumpscare Trigger" || n == "SK_FirstPersonPlayer_Grabpack_02.001")
            {
                foreach (Renderer r in t.GetComponentsInChildren<Renderer>(true))
                    r.enabled = false;
            }
        }
        foreach (TMPro.TextMeshPro label in clone.GetComponentsInChildren<TMPro.TextMeshPro>(true))
            label.gameObject.SetActive(false);

        return clone.transform;
    }

    private static void HideBoxProp()
    {
        Transform box = FindHuggyPreview();
        if (box != null)
            box.gameObject.SetActive(false);
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
