using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Sur les clients, renvoie automatiquement au menu Lobby si l'hôte ferme sa session.
/// Le composant reste vivant même si le GameManager réseau est détruit.
/// </summary>
public sealed class NetworkHostDisconnectReturn : MonoBehaviour
{
    private static NetworkHostDisconnectReturn instance;
    private NetworkManager subscribedManager;
    private bool wasConnectedAsClient;
    private bool returningToMenu;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (instance != null) return;
        GameObject root = new GameObject("NetworkHostDisconnectReturn");
        instance = root.AddComponent<NetworkHostDisconnectReturn>();
        DontDestroyOnLoad(root);
    }

    private void Update()
    {
        NetworkManager manager = NetworkManager.Singleton;
        if (manager == null) return;

        if (subscribedManager != manager)
        {
            Unsubscribe();
            subscribedManager = manager;
            subscribedManager.OnClientDisconnectCallback += OnClientDisconnected;
        }

        if (manager.IsClient && !manager.IsServer && manager.IsListening)
            wasConnectedAsClient = true;
    }

    private void OnClientDisconnected(ulong clientId)
    {
        NetworkManager manager = subscribedManager;
        if (returningToMenu || !wasConnectedAsClient || manager == null || manager.IsServer)
            return;

        // Sur un client, la perte du serveur/hôte est signalée avec ServerClientId (0).
        if (clientId != NetworkManager.ServerClientId)
            return;

        returningToMenu = true;
        Debug.LogWarning("[Network] L'hôte a quitté la partie. Retour au menu principal.");
        Unsubscribe();
        if (manager.IsListening)
            manager.Shutdown();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (SceneManager.GetActiveScene().name != "Lobby")
            SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
    }

    private void Unsubscribe()
    {
        if (subscribedManager != null)
            subscribedManager.OnClientDisconnectCallback -= OnClientDisconnected;
        subscribedManager = null;
    }

    private void OnDestroy()
    {
        Unsubscribe();
        if (instance == this) instance = null;
    }
}
