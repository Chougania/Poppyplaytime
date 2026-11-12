using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>In-game pause overlay. Does not pause network simulation or other players.</summary>
public class PoppyPauseMenu : MonoBehaviour
{
    private static PoppyPauseMenu instance;
    private Canvas canvas;
    private bool isOpen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (instance != null) return;
        GameObject root = new GameObject("PoppyPauseMenu_Runtime");
        instance = root.AddComponent<PoppyPauseMenu>();
        DontDestroyOnLoad(root);
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        Build();
        SetOpen(false);
    }

    private void Update()
    {
        NetworkManager network = NetworkManager.Singleton;
        bool inGameSession = network != null && network.IsListening && network.IsConnectedClient;
        if (!inGameSession)
        {
            if (isOpen) SetOpen(false);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!isOpen && (PoppyUIInputGate.IsBlocked || PoppyUIInputGate.IsPointerOverUI()))
                return;
            SetOpen(!isOpen);
        }
    }

    private void Build()
    {
        GameObject root = new GameObject("Pause Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject shade = new GameObject("Dim background", typeof(RectTransform), typeof(Image));
        shade.transform.SetParent(root.transform, false);
        RectTransform shadeRect = shade.GetComponent<RectTransform>();
        shadeRect.anchorMin = Vector2.zero; shadeRect.anchorMax = Vector2.one;
        shadeRect.offsetMin = Vector2.zero; shadeRect.offsetMax = Vector2.zero;
        shade.GetComponent<Image>().color = new Color(0.005f, 0.008f, 0.012f, 0.84f);

        GameObject panel = new GameObject("Pause panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(root.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.33f, 0.18f); panelRect.anchorMax = new Vector2(0.67f, 0.82f);
        panelRect.offsetMin = Vector2.zero; panelRect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.025f, 0.035f, 0.045f, 0.98f);

        Label(panel.transform, "PAUSE", 38, new Vector2(0.08f, 0.76f), new Vector2(0.92f, 0.94f), Color.white);
        Label(panel.transform, "TAKE A BREATH. IT IS STILL OUT THERE.", 13,
            new Vector2(0.08f, 0.68f), new Vector2(0.92f, 0.76f), new Color(0.8f, 0.32f, 0.22f));
        MakeButton(panel.transform, "CONTINUE", new Vector2(0.12f, 0.52f), new Vector2(0.88f, 0.64f), () => SetOpen(false));
        MakeButton(panel.transform, "RETURN TO LOBBY", new Vector2(0.12f, 0.36f), new Vector2(0.88f, 0.48f), ReturnToLobby);
        MakeButton(panel.transform, "QUIT TO DESKTOP", new Vector2(0.12f, 0.20f), new Vector2(0.88f, 0.32f), () => Application.Quit());
        Label(panel.transform, "ESC  •  RESUME", 12, new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.14f), new Color(0.55f, 0.6f, 0.65f));
        EnsureEventSystem();
    }

    private static void EnsureEventSystem()
    {
        EventSystem existing = FindObjectOfType<EventSystem>();
        if (existing == null)
        {
            GameObject eventRoot = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(eventRoot);
            return;
        }

        EventSystem[] all = FindObjectsOfType<EventSystem>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != existing)
                Destroy(all[i].gameObject);
        }
    }

    private void ReturnToLobby()
    {
        GameManager manager = FindObjectOfType<GameManager>();
        if (manager != null)
            manager.RequestReturnToLobby();
        SetOpen(false);
    }

    private void SetOpen(bool open)
    {
        isOpen = open;
        if (canvas != null) canvas.gameObject.SetActive(open);
        PoppyUIInputGate.SetPauseOpen(open);
        if (open)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            bool anotherUIOwnsCursor = PoppyUIInputGate.IsBlocked;
            Cursor.lockState = anotherUIOwnsCursor ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = anotherUIOwnsCursor;
        }
    }

    private static void Label(Transform parent, string value, float size, Vector2 min, Vector2 max, Color color)
    {
        GameObject obj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        TextMeshProUGUI label = obj.GetComponent<TextMeshProUGUI>();
        label.text = value; label.fontSize = size; label.color = color; label.alignment = TextAlignmentOptions.Center;
        if (TMP_Settings.defaultFontAsset != null) label.font = TMP_Settings.defaultFontAsset;
    }

    private static void MakeButton(Transform parent, string value, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action)
    {
        GameObject obj = new GameObject(value, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        Image image = obj.GetComponent<Image>();
        image.color = new Color(0.25f, 0.045f, 0.035f, 1f);
        Button button = obj.GetComponent<Button>();
        button.targetGraphic = image; button.onClick.AddListener(action);
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.52f, 0.08f, 0.045f, 1f);
        colors.pressedColor = new Color(0.12f, 0.025f, 0.02f, 1f);
        button.colors = colors;
        Label(obj.transform, value, 18, Vector2.zero, Vector2.one, Color.white);
    }
}
