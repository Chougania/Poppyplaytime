using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Legacy scene helper. Kept for serialized scene compatibility; match timer and spawn
/// ownership now live in GameManager/ObjectiveWorld to avoid duplicate HUD overlays.
/// </summary>
[DefaultExecutionOrder(-1000)]
public sealed class PlayerSpawnAndTimerFix : MonoBehaviour
{
    private static PlayerSpawnAndTimerFix instance;
    private GameManager gameManager;
    private bool previousMatchState;
    private bool correctingSpawns;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (instance != null)
            return;

        GameObject runner = new GameObject("Poppy player spawn and timer fix");
        DontDestroyOnLoad(runner);
        instance = runner.AddComponent<PlayerSpawnAndTimerFix>();
    }

    private void Update()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
            return;

        bool matchStarted = gameManager.IsGameStarted;
        if (matchStarted && !previousMatchState && !correctingSpawns &&
            NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            StartCoroutine(CorrectMatchSpawns());
        previousMatchState = matchStarted;
    }

    private IEnumerator CorrectMatchSpawns()
    {
        correctingSpawns = true;
        yield return new WaitForSecondsRealtime(1.25f);
        NetworkManager network = NetworkManager.Singleton;
        if (gameManager == null || network == null || !network.IsServer || !gameManager.IsGameStarted)
        {
            correctingSpawns = false;
            yield break;
        }

        List<Transform> survivorPoints = new List<Transform>();
        if (gameManager.survivorSpawnPoints != null)
            for (int i = 0; i < gameManager.survivorSpawnPoints.Length; i++)
                if (gameManager.survivorSpawnPoints[i] != null)
                    survivorPoints.Add(gameManager.survivorSpawnPoints[i]);
        if (survivorPoints.Count == 0 && gameManager.playerSpawnPoint != null)
            survivorPoints.Add(gameManager.playerSpawnPoint);

        int survivorIndex = 0;
        foreach (NetworkClient client in network.ConnectedClientsList)
        {
            if (client == null || client.PlayerObject == null || !client.PlayerObject.IsSpawned)
                continue;
            DbUnityMovment movement = client.PlayerObject.GetComponent<DbUnityMovment>();
            bool isMonster = movement != null && movement.monsterModelContainer != null && movement.monsterModelContainer.activeSelf;
            if (isMonster)
            {
                Transform monsterPoint = gameManager.playerMonsterSpawnPoint != null
                    ? gameManager.playerMonsterSpawnPoint : gameManager.monsterSpawnPoint;
                if (monsterPoint != null)
                    gameManager.ExecuteTargetedTeleport(client.ClientId, monsterPoint.position, true, false);
            }
            else if (survivorPoints.Count > 0)
            {
                Transform spawn = survivorPoints[survivorIndex % survivorPoints.Count];
                gameManager.ExecuteTargetedTeleport(client.ClientId, spawn.position, false, true);
                survivorIndex++;
            }
        }
        correctingSpawns = false;
    }
}
