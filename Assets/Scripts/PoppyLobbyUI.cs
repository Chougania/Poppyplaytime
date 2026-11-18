using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lightweight multiplayer waiting overlay. All 3D waiting-room content is authored manually in Unity.
/// P toggles ready; O starts the match for the host.
/// </summary>
public class PoppyLobbyUI : MonoBehaviour
{
    [Header("References")]
    public GameManager gameManager;
    public ConnectionManafer connectionManager;

    private Canvas canvas;
    private RectTransform rosterRoot;
    private TextMeshProUGUI statusText;
    private TextMeshProUGUI modeText;
    private TextMeshProUGUI walletText;
    private TextMeshProUGUI roomCodeText;
    private Button readyButton;
    private Button startButton;
    private bool localReady;
    private bool wasInSession;
    private bool wasInMatch;
    private bool isVisible;
    private bool userHidden;
    private float rosterRefreshTimer;

    private void Start()
    {
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        if (connectionManager == null) connectionManager = FindObjectOfType<ConnectionManafer>();
        if (connectionManager != null && connectionManager.canvas2 != null)
            connectionManager.canvas2.SetActive(false);
        BuildUI();
        SetVisible(false, false);
    }

    private void Update()
    {
        NetworkManager net = NetworkManager.Singleton;
        bool connected = net != null && net.IsListening && net.IsConnectedClient;
        if (connected != wasInSession)
        {
            wasInSession = connected;
            localReady = false;
        }

        if (!connected)
        {
            if (isVisible) SetVisible(false, false);
            return;
        }

        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        bool inMatch = gameManager != null && gameManager.IsGameStarted;
        // Automatically launch AI mode when the host is alone; no second player or ready click is needed.
        if (!inMatch && net.IsHost && gameManager != null &&
            PlayerPrefs.GetInt("Poppy.EnemyMode", 0) == 1 &&
            net.ConnectedClientsList.Count == 1)
        {
            gameManager.TryStartSoloMatchFromLobby();
        }
        if (inMatch != wasInMatch)
        {
            wasInMatch = inMatch;
            localReady = false;
            if (readyButton != null)
            {
                TMP_Text readyLabel = readyButton.GetComponentInChildren<TMP_Text>();
                if (readyLabel != null) readyLabel.text = "JE SUIS PRÊT";
            }
        }
        // TAB masque / réaffiche le salon : masqué, le curseur est verrouillé et la caméra + le Grabpack fonctionnent.
        if (inMatch) userHidden = false;
        else if (Input.GetKeyDown(KeyCode.Tab)) userHidden = !userHidden;

        bool shouldShow = !inMatch && !userHidden;
        if (shouldShow != isVisible)
            SetVisible(shouldShow, true);

        if (!inMatch)
        {
            if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.R)) ToggleReady();
            if (net.IsHost && Input.GetKeyDown(KeyCode.O)) StartMatch();
        }

        if (!isVisible)
            return;

        rosterRefreshTimer -= Time.unscaledDeltaTime;
        if (rosterRefreshTimer <= 0f)
        {
            rosterRefreshTimer = 0.5f;
            RefreshRoster();
        }
        statusText.text = inMatch ? "PARTIE EN COURS  •  BONNE CHANCE" : "EN ATTENTE DES JOUEURS  •  TAB : MASQUER LE SALON";
        modeText.text = PlayerPrefs.GetInt("Poppy.EnemyMode", 0) == 1
            ? "MODE : HUGGY IA  •  " + DifficultyName()
            : "MODE : MONSTRE JOUEUR";
        if (walletText != null)
            walletText.text = "CREDITS  " + PoppyShop.Credits + "     ★ TICKETS  " + PoppyShop.Tickets;
        if (roomCodeText != null)
        {
            bool showCode = net.IsHost && connectionManager != null;
            roomCodeText.gameObject.SetActive(showCode);
            if (showCode)
            {
                string code = connectionManager.CurrentRoomCode;
                roomCodeText.text = string.IsNullOrEmpty(code) ? "ROOM CODE  •  PREPARING..." : "ROOM CODE  •  " + code;
            }
        }

        if (readyButton != null) readyButton.interactable = !inMatch;
        if (startButton != null)
        {
            startButton.gameObject.SetActive(net.IsHost && !inMatch);
            startButton.interactable = gameManager != null && gameManager.CanStartMatch;
        }
    }

    private string DifficultyName()
    {
        string[] values = { "FACILE", "NORMAL", "CAUCHEMAR" };
        return values[Mathf.Clamp(PlayerPrefs.GetInt("Poppy.AIDifficulty", 1), 0, 2)];
    }

    private void BuildUI()
    {
        GameObject root = new GameObject("PoppyLobbyCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject codePlate = Panel("Host room code plate", root.transform, new Vector2(0.015f, 0.015f), new Vector2(0.38f, 0.10f), new Color(0.018f, 0.025f, 0.035f, 0.94f));
        roomCodeText = Label("ROOM CODE  •  WAITING...", root.transform, new Vector2(0.03f, 0.025f), new Vector2(0.36f, 0.085f), 19, new Color(1f, 0.78f, 0.24f));
        roomCodeText.gameObject.SetActive(false);
        GameObject panel = Panel("Lobby glass panel", root.transform, new Vector2(0.62f, 0.12f), new Vector2(0.97f, 0.88f),
            new Color(0.018f, 0.025f, 0.035f, 0.96f));
        Label("PLAYTIME CO.  /  MATCH ROOM", panel.transform, new Vector2(0.07f, 0.83f), new Vector2(0.93f, 0.96f), 30, Color.white);
        walletText = Label("CREDITS  " + PoppyShop.Credits + "     ★ TICKETS  " + PoppyShop.Tickets,
            panel.transform, new Vector2(0.07f, 0.76f), new Vector2(0.93f, 0.82f), 15, new Color(1f, 0.78f, 0.24f));
        statusText = Label("EN ATTENTE DES JOUEURS", panel.transform, new Vector2(0.07f, 0.69f), new Vector2(0.93f, 0.75f), 17, new Color(1f, 0.35f, 0.2f));
        modeText = Label("", panel.transform, new Vector2(0.07f, 0.62f), new Vector2(0.93f, 0.68f), 15, new Color(0.65f, 0.8f, 1f));
        Label("JOUEURS CONNECTÉS", panel.transform, new Vector2(0.07f, 0.54f), new Vector2(0.93f, 0.60f), 16, new Color(0.8f, 0.83f, 0.86f));

        GameObject roster = new GameObject("Player roster", typeof(RectTransform), typeof(VerticalLayoutGroup));
        roster.transform.SetParent(panel.transform, false);
        rosterRoot = roster.GetComponent<RectTransform>();
        rosterRoot.anchorMin = new Vector2(0.07f, 0.25f);
        rosterRoot.anchorMax = new Vector2(0.93f, 0.53f);
        rosterRoot.offsetMin = Vector2.zero;
        rosterRoot.offsetMax = Vector2.zero;
        VerticalLayoutGroup layout = roster.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 8;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;

        Label("P / R  •  SE DÉCLARER PRÊT     O  •  LANCER (HÔTE)", panel.transform,
            new Vector2(0.07f, 0.035f), new Vector2(0.93f, 0.095f), 13, new Color(0.72f, 0.78f, 0.84f));

        readyButton = ButtonUI("READY BUTTON", panel.transform, "P / R  •  JE SUIS PRÊT",
            new Vector2(0.07f, 0.12f), new Vector2(0.48f, 0.22f), ToggleReady);
        startButton = ButtonUI("START BUTTON", panel.transform, "O  •  LANCER LA PARTIE",
            new Vector2(0.52f, 0.12f), new Vector2(0.93f, 0.22f), StartMatch);
    }

    private void RefreshRoster()
    {
        if (rosterRoot == null || NetworkManager.Singleton == null) return;
        foreach (Transform child in rosterRoot) Destroy(child.gameObject);

        // ConnectedClientsList n'existe que sur le serveur : un client lisait cette liste et déclenchait une erreur.
        if (NetworkManager.Singleton.IsServer)
        {
            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
                AddRosterRow(client.ClientId, client.PlayerObject != null ? client.PlayerObject.gameObject : null);
        }
        else
        {
            foreach (DbUnityMovment player in FindObjectsOfType<DbUnityMovment>())
            {
                if (player != null && player.NetworkObject != null && player.NetworkObject.IsSpawned)
                    AddRosterRow(player.OwnerClientId, player.gameObject);
            }
        }
    }

    private void AddRosterRow(ulong clientId, GameObject playerObject)
    {
        bool isReady = gameManager != null && gameManager.IsClientReady(clientId);
        bool isHost = clientId == NetworkManager.ServerClientId;
        string playerName = "JOUEUR " + clientId;
        if (playerObject != null)
        {
            PlayerNameTag tag = playerObject.GetComponent<PlayerNameTag>();
            if (tag != null && tag.nameTextMesh != null && !string.IsNullOrWhiteSpace(tag.nameTextMesh.text))
                playerName = tag.nameTextMesh.text;
        }
        Label("Player " + clientId, rosterRoot, Vector2.zero, Vector2.one, 18,
            isReady ? new Color(0.45f, 0.9f, 0.58f) : new Color(0.88f, 0.88f, 0.88f)).text =
            (isHost ? "★  " : "    ") + playerName + "     " + (isReady ? "✓ PRÊT" : "… EN ATTENTE");
    }

    private void OnGUI()
    {
        // Le chronomètre est dessiné localement sur chaque instance (hôte ET clients).
        // Sa valeur provient de la NetworkVariable synchronisée par le serveur.
        if (wasInSession && wasInMatch && gameManager != null)
        {
            int left = Mathf.Max(0, gameManager.RoundTimeLeft);
            bool urgent = left <= 60;
            GUIStyle timerStyle = new GUIStyle(GUI.skin.label);
            timerStyle.fontSize = Mathf.Clamp(Screen.height / 24, 26, 42);
            timerStyle.fontStyle = FontStyle.Bold;
            timerStyle.alignment = TextAnchor.UpperCenter;
            float pulse = urgent ? 0.65f + 0.35f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)) : 1f;
            timerStyle.normal.textColor = urgent
                ? new Color(1f, 0.25f * pulse, 0.2f * pulse)
                : Color.white;
            GUI.Label(new Rect(0f, 12f, Screen.width, 54f),
                (left / 60).ToString("00") + ":" + (left % 60).ToString("00"), timerStyle);
        }

        if (!wasInSession || wasInMatch || !userHidden)
            return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 18;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(0f, Screen.height - 50f, Screen.width, 30f),
            "[TAB] Réafficher le salon   •   [P] Prêt   •   [O] Lancer (hôte)", style);
    }

    private void ToggleReady()
    {
        localReady = !localReady;
        NetworkClient localClient = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClient : null;
        DbUnityMovment me = localClient != null && localClient.PlayerObject != null
            ? localClient.PlayerObject.GetComponent<DbUnityMovment>() : null;
        if (me == null || !me.RequestReady(localReady))
        {
            if (gameManager != null) gameManager.SetLocalReady(localReady);
        }
        if (readyButton != null)
        {
            TMP_Text label = readyButton.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = localReady ? "P / R  •  ANNULER PRÊT" : "P / R  •  JE SUIS PRÊT";
        }
    }

    private void StartMatch()
    {
        if (gameManager != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
            gameManager.StartMatchFromLobby();
    }

    private void SetVisible(bool visible, bool sessionActive)
    {
        isVisible = visible;
        PoppyUIInputGate.SetLobbyOpen(visible);
        if (canvas != null) canvas.gameObject.SetActive(visible);
        if (visible)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else if (sessionActive)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private GameObject Panel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        obj.GetComponent<Image>().color = color;
        return obj;
    }

    private TextMeshProUGUI Label(string value, Transform parent, Vector2 min, Vector2 max, float size, Color color)
    {
        GameObject obj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
        text.text = value; text.fontSize = size; text.color = color;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = false;
        if (TMP_Settings.defaultFontAsset != null) text.font = TMP_Settings.defaultFontAsset;
        return text;
    }

    private Button ButtonUI(string name, Transform parent, string caption, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        Image image = obj.GetComponent<Image>();
        image.color = new Color(0.34f, 0.055f, 0.035f, 1f);
        Button button = obj.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        Label(caption, obj.transform, Vector2.zero, Vector2.one, 18, Color.white).alignment = TextAlignmentOptions.Center;
        return button;
    }
}
