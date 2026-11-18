using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Safety net for match placement and timer visibility. The authoritative GameManager
/// still assigns roles; this retries their spawn positions once player roles are applied.
/// </summary>
[DefaultExecutionOrder(-1000)]
public sealed class PlayerSpawnAndTimerFix : MonoBehaviour
{
    private static PlayerSpawnAndTimerFix instance;
    private GameManager gameManager;
    private bool previousMatchState;
    private bool correctingSpawns;
    private GUIStyle timerStyle;

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
        {
            StartCoroutine(CorrectMatchSpawns());
        }
        previousMatchState = matchStarted;
    }

    private IEnumerator CorrectMatchSpawns()
    {
        correctingSpawns = true;
        // Let role/model changes and initial spawn RPCs settle before retrying placements.
        yield return new WaitForSecondsRealtime(1.25f);

        NetworkManager network = NetworkManager.Singleton;
        if (gameManager == null || network == null || !network.IsServer || !gameManager.IsGameStarted)
        {
            correctingSpawns = false;
            yield break;
        }

        List<Transform> survivorPoints = new List<Transform>();
        if (gameManager.survivorSpawnPoints != null)
        {
            for (int i = 0; i < gameManager.survivorSpawnPoints.Length; i++)
            {
                if (gameManager.survivorSpawnPoints[i] != null)
                    survivorPoints.Add(gameManager.survivorSpawnPoints[i]);
            }
        }

        // The legacy single survivor marker remains supported, but no longer forces
        // every survivor onto one position when an array of points is configured.
        if (survivorPoints.Count == 0 && gameManager.playerSpawnPoint != null)
            survivorPoints.Add(gameManager.playerSpawnPoint);

        int survivorIndex = 0;
        foreach (NetworkClient client in network.ConnectedClientsList)
        {
            if (client == null || client.PlayerObject == null || !client.PlayerObject.IsSpawned)
                continue;

            DbUnityMovment movement = client.PlayerObject.GetComponent<DbUnityMovment>();
            bool isMonster = movement != null && movement.monsterModelContainer != null &&
                movement.monsterModelContainer.activeSelf;
            if (isMonster)
            {
                Transform monsterPoint = gameManager.playerMonsterSpawnPoint != null
                    ? gameManager.playerMonsterSpawnPoint
                    : gameManager.monsterSpawnPoint;
                if (monsterPoint != null)
                    gameManager.ExecuteTargetedTeleport(client.ClientId, monsterPoint.position, true, false);
                continue;
            }

            if (survivorPoints.Count > 0)
            {
                Transform spawn = survivorPoints[survivorIndex % survivorPoints.Count];
                gameManager.ExecuteTargetedTeleport(client.ClientId, spawn.position, false, true);
                survivorIndex++;
            }
        }

        correctingSpawns = false;
    }

    private void OnGUI()
    {
        if (gameManager == null || !gameManager.IsGameStarted)
            return;

        if (timerStyle == null)
        {
            timerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Clamp(Screen.height / 24, 28, 48),
                fontStyle = FontStyle.Bold,
                richText = false
            };
        }

        int seconds = Mathf.Max(0, gameManager.RoundTimeLeft);
        timerStyle.normal.textColor = seconds <= 60
            ? (Mathf.Sin(Time.unscaledTime * 5f) > 0f ? new Color(1f, 0.25f, 0.2f) : Color.white)
            : Color.white;
        GUI.Label(new Rect(0f, 12f, Screen.width, 56f),
            (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00"), timerStyle);
    }
}
