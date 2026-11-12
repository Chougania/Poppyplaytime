using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime lobby overlay: player roster, ready state, host start and match status.
/// Attach to a scene object in the Lobby scene, or let PoppyMenuBootstrap create it.
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
    private Button readyButton;
    private Button startButton;
    private bool localReady;
    private bool wasInSession;

    private void Start()
    {
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        if (connectionManager == null) connectionManager = FindObjectOfType<ConnectionManafer>();
        BuildUI();
        SetVisible(false);
    }

    private void Update()
    {
        NetworkManager net = NetworkManager.Singleton;
        bool connected = net != null && net.IsListening && net.IsConnectedClient;
        if (connected != wasInSession)
        {
            wasInSession = connected;
            SetVisible(connected);
            localReady = false;
        }

        if (!connected || canvas == null || !canvas.gameObject.activeSelf)
            return;

        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        RefreshRoster();
        bool inMatch = gameManager != null && gameManager.IsGameStarted;
        statusText.text = inMatch ? "PARTIE EN COURS  •  BONNE CHANCE" : "EN ATTENTE DES JOUEURS";
        modeText.text = PlayerPrefs.GetInt("Poppy.EnemyMode", 0) == 1
            ? "MODE : HUGGY IA  •  " + DifficultyName()
            : "MODE : MONSTRE JOUEUR";

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

        GameObject panel = Panel("Lobby glass panel", root.transform, new Vector2(0.62f, 0.12f), new Vector2(0.97f, 0.88f),
            new Color(0.018f, 0.025f, 0.035f, 0.96f));
        Label("LOBBY", panel.transform, new Vector2(0.07f, 0.83f), new Vector2(0.93f, 0.96f), 38, Color.white);
        statusText = Label("EN ATTENTE DES JOUEURS", panel.transform, new Vector2(0.07f, 0.75f), new Vector2(0.93f, 0.82f), 17, new Color(1f, 0.35f, 0.2f));
        modeText = Label("", panel.transform, new Vector2(0.07f, 0.67f), new Vector2(0.93f, 0.74f), 15, new Color(0.65f, 0.8f, 1f));
        Label("JOUEURS CONNECTÉS", panel.transform, new Vector2(0.07f, 0.58f), new Vector2(0.93f, 0.65f), 16, new Color(0.8f, 0.83f, 0.86f));

        GameObject roster = new GameObject("Player roster", typeof(RectTransform), typeof(VerticalLayoutGroup));
        roster.transform.SetParent(panel.transform, false);
        rosterRoot = roster.GetComponent<RectTransform>();
        rosterRoot.anchorMin = new Vector2(0.07f, 0.25f);
        rosterRoot.anchorMax = new Vector2(0.93f, 0.57f);
        rosterRoot.offsetMin = Vector2.zero;
        rosterRoot.offsetMax = Vector2.zero;
        VerticalLayoutGroup layout = roster.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 8;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;

        readyButton = ButtonUI("READY BUTTON", panel.transform, "PRÊT / PAS PRÊT",
            new Vector2(0.07f, 0.12f), new Vector2(0.48f, 0.22f), ToggleReady);
        startButton = ButtonUI("START BUTTON", panel.transform, "LANCER LA PARTIE",
            new Vector2(0.52f, 0.12f), new Vector2(0.93f, 0.22f), StartMatch);
    }

    private void RefreshRoster()
    {
        if (rosterRoot == null || NetworkManager.Singleton == null) return;
        foreach (Transform child in rosterRoot) Destroy(child.gameObject);

        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            bool isReady = gameManager != null && gameManager.IsClientReady(client.ClientId);
            bool isHost = client.ClientId == NetworkManager.ServerClientId;
            string playerName = "JOUEUR " + client.ClientId;
            if (client.PlayerObject != null)
            {
                PlayerNameTag tag = client.PlayerObject.GetComponent<PlayerNameTag>();
                if (tag != null && tag.nameTextMesh != null && !string.IsNullOrWhiteSpace(tag.nameTextMesh.text))
                    playerName = tag.nameTextMesh.text;
            }
            Label("Player " + client.ClientId, rosterRoot, Vector2.zero, Vector2.one, 18,
                isReady ? new Color(0.45f, 0.9f, 0.58f) : new Color(0.88f, 0.88f, 0.88f)).text =
                (isHost ? "★  " : "    ") + playerName + "     " + (isReady ? "✓ PRÊT" : "… EN ATTENTE");
        }
    }

    private void ToggleReady()
    {
        localReady = !localReady;
        if (gameManager != null) gameManager.SetLocalReady(localReady);
        if (readyButton != null)
        {
            TMP_Text label = readyButton.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = localReady ? "ANNULER PRÊT" : "JE SUIS PRÊT";
        }
    }

    private void StartMatch()
    {
        if (gameManager != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
            gameManager.StartMatchFromLobby();
    }

    private void SetVisible(bool visible)
    {
        if (canvas != null) canvas.gameObject.SetActive(visible);
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
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
