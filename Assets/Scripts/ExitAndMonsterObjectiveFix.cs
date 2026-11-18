using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Retries exit requests until the server accepts the survivor escape, synchronizes
/// the owner's lobby teleport, and hides only generator labels from the player monster.
/// Generator props and the exit remain visible to everyone.
/// </summary>
[DefaultExecutionOrder(-900)]
public sealed class ExitAndMonsterObjectiveFix : MonoBehaviour
{
    private static ExitAndMonsterObjectiveFix instance;
    private GameManager gameManager;
    private float nextExitRequestAt;
    private float nextVisibilityRefreshAt;
    private bool exitTeleportPending;
    private readonly List<GameObject> generatorLabels = new List<GameObject>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (instance != null)
            return;

        GameObject runner = new GameObject("Poppy exit and monster objective fix");
        DontDestroyOnLoad(runner);
        instance = runner.AddComponent<ExitAndMonsterObjectiveFix>();
    }

    private void Update()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            return;

        RefreshGeneratorLabelVisibility();
        TryRequestExit();
    }

    private void TryRequestExit()
    {
        if (!gameManager.IsGameStarted || exitTeleportPending || Time.unscaledTime < nextExitRequestAt)
            return;

        NetworkManager network = NetworkManager.Singleton;
        if (network.LocalClient == null || network.LocalClient.PlayerObject == null)
            return;

        DbUnityMovment localPlayer = network.LocalClient.PlayerObject.GetComponent<DbUnityMovment>();
        if (!IsLocalSurvivor(localPlayer) || localPlayer.IsBoxed || !gameManager.ExitOpen)
            return;

        if ((localPlayer.transform.position - gameManager.ExitPos).sqrMagnitude > 4.4f * 4.4f)
            return;

        nextExitRequestAt = Time.unscaledTime + 0.75f;
        // Request acceptance while the player is still at the exit; then ensure the
        // owner-authoritative transform and the custom interpolation variables agree.
        gameManager.EscapeServerRpc();
        if (gameManager.lobbySpawnPoint != null)
            StartCoroutine(ApplyLobbyTeleport(localPlayer));
    }

    private IEnumerator ApplyLobbyTeleport(DbUnityMovment player)
    {
        exitTeleportPending = true;
        yield return new WaitForSecondsRealtime(0.2f);

        if (player != null && player.IsSpawned && gameManager != null && gameManager.lobbySpawnPoint != null)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            bool controllerWasEnabled = controller != null && controller.enabled;
            if (controllerWasEnabled) controller.enabled = false;

            Transform lobby = gameManager.lobbySpawnPoint;
            player.transform.SetPositionAndRotation(lobby.position, lobby.rotation);
            player.NetPosition.Value = lobby.position;
            player.NetRotation.Value = lobby.rotation;

            Rigidbody body = player.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.position = lobby.position;
                body.rotation = lobby.rotation;
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            Physics.SyncTransforms();
            if (controllerWasEnabled) controller.enabled = true;
        }

        yield return new WaitForSecondsRealtime(0.35f);
        exitTeleportPending = false;
    }

    private void RefreshGeneratorLabelVisibility()
    {
        if (Time.unscaledTime < nextVisibilityRefreshAt)
            return;
        nextVisibilityRefreshAt = Time.unscaledTime + 0.4f;

        NetworkManager network = NetworkManager.Singleton;
        DbUnityMovment localPlayer = network != null && network.LocalClient != null &&
            network.LocalClient.PlayerObject != null
            ? network.LocalClient.PlayerObject.GetComponent<DbUnityMovment>()
            : null;
        bool hideLabels = gameManager.IsGameStarted && IsLocalMonster(localPlayer);

        generatorLabels.Clear();
        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
        foreach (GameObject obj in allObjects)
        {
            if (obj == null || obj.name != "Label")
                continue;
            Transform parent = obj.transform.parent;
            if (parent != null && parent.name.StartsWith("Generator"))
                generatorLabels.Add(obj);
        }

        foreach (GameObject label in generatorLabels)
        {
            if (label != null && label.activeSelf == hideLabels)
                label.SetActive(!hideLabels);
        }
    }

    private static bool IsLocalMonster(DbUnityMovment player)
    {
        return player != null && player.monsterModelContainer != null && player.monsterModelContainer.activeSelf;
    }

    private static bool IsLocalSurvivor(DbUnityMovment player)
    {
        return player != null && player.survivorModelContainer != null && player.survivorModelContainer.activeSelf &&
            !IsLocalMonster(player);
    }
}
