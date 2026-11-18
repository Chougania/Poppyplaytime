using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Retries exit requests until the server accepts the survivor escape, and hides
/// local-only objective props from the player-controlled monster.
/// </summary>
[DefaultExecutionOrder(-900)]
public sealed class ExitAndMonsterObjectiveFix : MonoBehaviour
{
    private static ExitAndMonsterObjectiveFix instance;
    private GameManager gameManager;
    private float nextExitRequestAt;
    private float nextVisibilityRefreshAt;
    private readonly List<GameObject> objectiveObjects = new List<GameObject>();

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

        RefreshObjectiveVisibility();
        TryRequestExit();
    }

    private void TryRequestExit()
    {
        if (!gameManager.IsGameStarted || Time.unscaledTime < nextExitRequestAt)
            return;

        NetworkManager network = NetworkManager.Singleton;
        if (network.LocalClient == null || network.LocalClient.PlayerObject == null)
            return;

        DbUnityMovment localPlayer = network.LocalClient.PlayerObject.GetComponent<DbUnityMovment>();
        if (!IsLocalSurvivor(localPlayer) || localPlayer.IsBoxed)
            return;

        // The original one-shot request could be rejected while the owner position
        // was still being replicated to the server. Retry at the server's accepted range.
        if ((localPlayer.transform.position - gameManager.ExitPos).sqrMagnitude > 4.4f * 4.4f)
            return;

        nextExitRequestAt = Time.unscaledTime + 0.75f;
        gameManager.EscapeServerRpc();
    }

    private void RefreshObjectiveVisibility()
    {
        if (Time.unscaledTime < nextVisibilityRefreshAt)
            return;
        nextVisibilityRefreshAt = Time.unscaledTime + 0.4f;

        NetworkManager network = NetworkManager.Singleton;
        DbUnityMovment localPlayer = network != null && network.LocalClient != null &&
            network.LocalClient.PlayerObject != null
            ? network.LocalClient.PlayerObject.GetComponent<DbUnityMovment>()
            : null;
        bool hideObjectives = gameManager.IsGameStarted && IsLocalMonster(localPlayer);

        objectiveObjects.Clear();
        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
        foreach (GameObject obj in allObjects)
        {
            if (obj == null)
                continue;
            if (obj.name == "ExitGate" || obj.name.StartsWith("Generator"))
                objectiveObjects.Add(obj);
        }

        foreach (GameObject obj in objectiveObjects)
        {
            if (obj != null && obj.activeSelf == hideObjectives)
                obj.SetActive(!hideObjectives);
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
