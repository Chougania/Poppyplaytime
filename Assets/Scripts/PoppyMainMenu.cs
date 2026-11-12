using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Builds the creepy main-menu presentation at runtime. Assign the existing character
/// preview and Huggy model in the Inspector; no imported meshes or materials are required.
/// </summary>
public class PoppyMainMenu : MonoBehaviour
{
    [Header("Scene References")]
    public Camera menuCamera;
    public Transform characterPreview;
    public Transform huggyPreview;
    public ConnectionManafer connectionManager;

    [Header("Atmosphere")]
    public bool buildCorridorAtRuntime = true;
    [Min(15f)] public float corridorLength = 38f;
    public Color corridorTint = new Color(0.055f, 0.065f, 0.075f);
    public Color warningLightColor = new Color(1f, 0.035f, 0.02f);

    private Canvas menuCanvas;
    private TMP_InputField joinCodeInput;
    private TMP_InputField playerNameInput;
    private TextMeshProUGUI modeLabel;
    private GameObject shopPanel;
    private TextMeshProUGUI shopCreditsLabel;
    private TextMeshProUGUI shopStatusLabel;
    private bool isShopOpen;
    private Transform corridorRoot;
    private Material wallMaterial;
    private Material floorMaterial;

    private void Start()
    {
        if (menuCamera == null)
            menuCamera = Camera.main;
        if (connectionManager == null)
            connectionManager = FindObjectOfType<ConnectionManafer>();

        if (buildCorridorAtRuntime)
            BuildCreepyCorridor();

        BuildInterface();
        ApplyPreviewSkin();
    }

    private void Update()
    {
        if (connectionManager != null && connectionManager.networkManager != null &&
            connectionManager.networkManager.IsListening && menuCanvas != null && menuCanvas.gameObject.activeSelf)
        {
            menuCanvas.gameObject.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void BuildCreepyCorridor()
    {
        if (menuCamera == null)
        {
            Debug.LogWarning("PoppyMainMenu: assign a menu camera to build the corridor.", this);
            return;
        }

        GameObject root = new GameObject("POPPY_MENU_CREEPY_CORRIDOR");
        corridorRoot = root.transform;
        corridorRoot.position = transform.position;

        wallMaterial = CreateMaterial("Menu aged concrete", corridorTint);
        floorMaterial = CreateMaterial("Menu dark floor", new Color(0.018f, 0.022f, 0.027f));
        Material trimMaterial = CreateMaterial("Menu rusted trim", new Color(0.12f, 0.035f, 0.025f));
        Material stripeMaterial = CreateMaterial("Menu warning stripe", new Color(0.65f, 0.12f, 0.025f));

        CreateBlock("Corridor floor", new Vector3(0f, -0.15f, corridorLength * 0.5f),
            new Vector3(8f, 0.3f, corridorLength), floorMaterial);
        CreateBlock("Left wall", new Vector3(-4f, 2.5f, corridorLength * 0.5f),
            new Vector3(0.35f, 5f, corridorLength), wallMaterial);
        CreateBlock("Right wall", new Vector3(4f, 2.5f, corridorLength * 0.5f),
            new Vector3(0.35f, 5f, corridorLength), wallMaterial);
        CreateBlock("Ceiling", new Vector3(0f, 5.05f, corridorLength * 0.5f),
            new Vector3(8f, 0.25f, corridorLength), wallMaterial);
        CreateBlock("End wall", new Vector3(0f, 2.5f, corridorLength),
            new Vector3(8f, 5f, 0.35f), wallMaterial);

        for (int i = 0; i < 7; i++)
        {
            float z = 2f + i * 5.2f;
            CreateBlock("Left wall rib " + i, new Vector3(-3.72f, 2.5f, z),
                new Vector3(0.22f, 5f, 0.24f), trimMaterial);
            CreateBlock("Right wall rib " + i, new Vector3(3.72f, 2.5f, z),
                new Vector3(0.22f, 5f, 0.24f), trimMaterial);
            CreateBlock("Ceiling light housing " + i, new Vector3(0f, 4.83f, z),
                new Vector3(1.5f, 0.16f, 0.55f), trimMaterial);

            GameObject lightObject = new GameObject("Flickering fluorescent " + i);
            lightObject.transform.SetParent(corridorRoot, false);
            lightObject.transform.localPosition = new Vector3(0f, 4.55f, z);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = i >= 5 ? warningLightColor : new Color(0.55f, 0.72f, 0.82f);
            light.range = 10f;
            light.intensity = i >= 5 ? 3.2f : 1.8f;
            StartCoroutine(Flicker(light, i));
        }

        for (int i = 0; i < 5; i++)
        {
            float z = 5f + i * 6f;
            CreateBlock("Floor hazard stripe L " + i, new Vector3(-3.35f, 0.012f, z),
                new Vector3(0.18f, 0.025f, 1.2f), stripeMaterial);
            CreateBlock("Floor hazard stripe R " + i, new Vector3(3.35f, 0.012f, z),
                new Vector3(0.18f, 0.025f, 1.2f), stripeMaterial);
        }

        menuCamera.transform.position = corridorRoot.position + new Vector3(0f, 1.65f, -5.5f);
        menuCamera.transform.rotation = Quaternion.identity;
        menuCamera.fieldOfView = 58f;

        if (characterPreview != null)
        {
            characterPreview.gameObject.SetActive(true);
            characterPreview.position = corridorRoot.position + new Vector3(-1.45f, 0f, 1.2f);
            characterPreview.rotation = Quaternion.Euler(0f, 18f, 0f);
        }

        if (huggyPreview != null)
        {
            huggyPreview.gameObject.SetActive(true);
            huggyPreview.position = corridorRoot.position + new Vector3(0f, 0f, corridorLength - 4f);
            huggyPreview.rotation = Quaternion.Euler(0f, 180f, 0f);
            StartCoroutine(HuggyIdleMotion(huggyPreview));
        }
        else
        {
            Debug.LogWarning("PoppyMainMenu: assign the existing Huggy model to Huggy Preview to show him at the end of the corridor.", this);
        }
    }

    private IEnumerator Flicker(Light light, int seed)
    {
        float baseIntensity = light.intensity;
        Random.InitState(seed * 7919 + 41);
        while (light != null)
        {
            yield return new WaitForSecondsRealtime(Random.Range(1.8f, 5.5f));
            int flickers = Random.Range(2, 6);
            for (int i = 0; i < flickers && light != null; i++)
            {
                light.enabled = false;
                yield return new WaitForSecondsRealtime(Random.Range(0.035f, 0.12f));
                if (light == null) yield break;
                light.enabled = true;
                light.intensity = baseIntensity * Random.Range(0.55f, 1.25f);
                yield return new WaitForSecondsRealtime(Random.Range(0.04f, 0.16f));
            }
            if (light != null)
                light.intensity = baseIntensity;
        }
    }

    private IEnumerator HuggyIdleMotion(Transform huggy)
    {
        Vector3 start = huggy.position;
        while (huggy != null)
        {
            float sway = Mathf.Sin(Time.unscaledTime * 0.55f) * 0.025f;
            huggy.position = start + Vector3.up * (0.025f + Mathf.Abs(sway));
            yield return null;
        }
    }

    private Material CreateMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Lit");

        Material material = new Material(shader);
        material.name = materialName;
        material.color = color;
        return material;
    }

    private void CreateBlock(string blockName, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = blockName;
        block.transform.SetParent(corridorRoot, false);
        block.transform.localPosition = localPosition;
        block.transform.localScale = localScale;
        Renderer renderer = block.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
    }

    private void BuildInterface()
    {
        GameObject canvasObject = new GameObject("PoppyMainMenuCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        menuCanvas = canvasObject.GetComponent<Canvas>();
        menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        CreatePanel("Left menu glass", menuCanvas.transform, new Vector2(0.035f, 0.08f),
            new Vector2(0.39f, 0.92f), new Color(0.012f, 0.018f, 0.024f, 0.88f));

        CreateText("Poppy title", menuCanvas.transform, "PROJECT PLAYTIME\nREMADE",
            48, new Vector2(0.055f, 0.72f), new Vector2(0.34f, 0.18f),
            TextAlignmentOptions.Left, new Color(0.9f, 0.92f, 0.94f));

        CreateText("Menu subtitle", menuCanvas.transform, "THE FACTORY IS STILL AWAKE",
            16, new Vector2(0.06f, 0.68f), new Vector2(0.32f, 0.045f),
            TextAlignmentOptions.Left, new Color(0.85f, 0.16f, 0.12f));

        CreateText("Menu warning", menuCanvas.transform, "DON'T LET IT FIND YOU.",
            15, new Vector2(0.06f, 0.635f), new Vector2(0.32f, 0.04f),
            TextAlignmentOptions.Left, new Color(0.62f, 0.66f, 0.7f));

        playerNameInput = CreateInput("Player name", menuCanvas.transform, "PLAYER NAME",
            new Vector2(0.06f, 0.55f), new Vector2(0.31f, 0.055f));
        joinCodeInput = CreateInput("Join code", menuCanvas.transform, "RELAY CODE (OPTIONAL)",
            new Vector2(0.06f, 0.48f), new Vector2(0.31f, 0.055f));

        CreateButton("HOST BUTTON", menuCanvas.transform, "CREATE SESSION",
            new Vector2(0.06f, 0.385f), new Vector2(0.31f, 0.07f), HostSession);
        CreateButton("JOIN BUTTON", menuCanvas.transform, "JOIN SESSION / QUICK JOIN",
            new Vector2(0.06f, 0.30f), new Vector2(0.31f, 0.07f), JoinSession);

        modeLabel = CreateButton("MODE BUTTON", menuCanvas.transform, GetModeLabel(),
            new Vector2(0.06f, 0.215f), new Vector2(0.15f, 0.06f), ToggleEnemyMode);
        CreateButton("SHOP BUTTON", menuCanvas.transform, "TOY SHOP",
            new Vector2(0.22f, 0.215f), new Vector2(0.15f, 0.06f), OpenShop);
        CreateButton("QUIT BUTTON", menuCanvas.transform, "QUIT",
            new Vector2(0.06f, 0.13f), new Vector2(0.31f, 0.055f), QuitGame);

        CreateText("Menu footer", menuCanvas.transform, "PLAYTIME CO.  •  EXPERIMENTAL BUILD",
            11, new Vector2(0.055f, 0.045f), new Vector2(0.34f, 0.035f),
            TextAlignmentOptions.Left, new Color(0.42f, 0.46f, 0.5f));

        if (connectionManager != null)
        {
            if (connectionManager.codeInputField == null)
                connectionManager.codeInputField = joinCodeInput;
            if (connectionManager.nameInputField == null)
                connectionManager.nameInputField = playerNameInput;
        }

        BuildShopPanel();
    }

    private void BuildShopPanel()
    {
        shopPanel = CreatePanel("Shop panel", menuCanvas.transform, new Vector2(0.52f, 0.12f),
            new Vector2(0.96f, 0.88f), new Color(0.012f, 0.018f, 0.024f, 0.96f));
        shopPanel.SetActive(false);

        CreateText("Shop title", shopPanel.transform, "TOY CUSTOMIZATION",
            32, new Vector2(0.06f, 0.82f), new Vector2(0.88f, 0.12f),
            TextAlignmentOptions.Left, Color.white);

        shopCreditsLabel = CreateText("Shop credits", shopPanel.transform, "",
            18, new Vector2(0.06f, 0.74f), new Vector2(0.88f, 0.06f),
            TextAlignmentOptions.Left, new Color(1f, 0.78f, 0.24f));

        shopStatusLabel = CreateText("Shop status", shopPanel.transform, "SELECT A COLOR TO EQUIP OR UNLOCK",
            14, new Vector2(0.06f, 0.08f), new Vector2(0.88f, 0.07f),
            TextAlignmentOptions.Left, new Color(0.7f, 0.74f, 0.78f));

        for (int i = 0; i < PoppyShop.ColorCount; i++)
        {
            int colorIndex = i;
            int column = i % 2;
            int row = i / 2;
            Vector2 position = new Vector2(0.06f + column * 0.46f, 0.62f - row * 0.145f);
            CreateButton("Color " + i, shopPanel.transform, GetShopItemLabel(colorIndex),
                position, new Vector2(0.42f, 0.105f), () => BuyOrEquipColor(colorIndex));
        }

        CreateButton("Close shop", shopPanel.transform, "BACK",
            new Vector2(0.70f, 0.08f), new Vector2(0.24f, 0.07f), CloseShop);
        RefreshShop();
    }

    private string GetShopItemLabel(int index)
    {
        if (PoppyShop.IsUnlocked(index))
            return (PoppyShop.SelectedColor == index ? "✓ " : "") + PoppyShop.GetColorName(index) + "  •  OWNED";
        return PoppyShop.GetColorName(index) + "  •  " + PoppyShop.GetPrice(index) + " C";
    }

    private void RefreshShop()
    {
        if (shopCreditsLabel != null)
            shopCreditsLabel.text = "CREDITS  " + PoppyShop.Credits;
        if (shopStatusLabel != null)
            shopStatusLabel.text = "EQUIPPED: " + PoppyShop.GetColorName(PoppyShop.SelectedColor) +
                "     •     CREDITS ARE EARNED AFTER MATCHES";

        if (shopPanel == null)
            return;

        // Rebuild labels from the current saved unlock state.
        for (int i = 0; i < PoppyShop.ColorCount; i++)
        {
            Transform item = shopPanel.transform.Find("Color " + i);
            if (item == null)
                continue;
            TMP_Text label = item.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = GetShopItemLabel(i);
        }
    }

    private void BuyOrEquipColor(int index)
    {
        bool success = PoppyShop.BuyOrSelect(index);
        if (shopStatusLabel != null)
            shopStatusLabel.text = success
                ? "EQUIPPED: " + PoppyShop.GetColorName(index)
                : "NOT ENOUGH CREDITS.";
        RefreshShop();
        ApplyPreviewSkin();
    }

    private void ApplyPreviewSkin()
    {
        if (characterPreview != null)
            PoppyShop.ApplyColor(characterPreview.gameObject, PoppyShop.SelectedColor);
    }

    private void OpenShop()
    {
        isShopOpen = true;
        shopPanel.SetActive(true);
        RefreshShop();
    }

    private void CloseShop()
    {
        isShopOpen = false;
        shopPanel.SetActive(false);
    }

    private void HostSession()
    {
        if (connectionManager == null)
        {
            Debug.LogError("PoppyMainMenu: ConnectionManafer is missing.", this);
            return;
        }

        ConnectionManafer.PlayerName = playerNameInput.text.Trim();
        connectionManager.HostAServer();
    }

    private void JoinSession()
    {
        if (connectionManager == null)
        {
            Debug.LogError("PoppyMainMenu: ConnectionManafer is missing.", this);
            return;
        }

        ConnectionManafer.PlayerName = playerNameInput.text.Trim();
        connectionManager.codeInputField = joinCodeInput;
        if (string.IsNullOrWhiteSpace(joinCodeInput.text))
            connectionManager.JoinRandomServer();
        else
            connectionManager.JoinWithCode();
    }

    private void ToggleEnemyMode()
    {
        int nextMode = PlayerPrefs.GetInt("Poppy.EnemyMode", 0) == 0 ? 1 : 0;
        PlayerPrefs.SetInt("Poppy.EnemyMode", nextMode);
        PlayerPrefs.Save();
        if (modeLabel != null)
            modeLabel.text = GetModeLabel();
    }

    private string GetModeLabel()
    {
        return PlayerPrefs.GetInt("Poppy.EnemyMode", 0) == 0
            ? "ENEMY: PLAYER"
            : "ENEMY: AI";
    }

    private void QuitGame()
    {
        Application.Quit();
    }

    private GameObject CreatePanel(string panelName, Transform parent, Vector2 anchorMin,
        Vector2 anchorMax, Color color)
    {
        GameObject panel = new GameObject(panelName, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private TextMeshProUGUI CreateText(string objectName, Transform parent, string value,
        float fontSize, Vector2 anchorMin, Vector2 anchorMax, TextAlignmentOptions alignment, Color color)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;
        return text;
    }

    private Button CreateButton(string objectName, Transform parent, string label,
        Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.16f, 0.035f, 0.03f, 0.94f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.18f, 0.045f, 0.04f, 0.95f);
        colors.highlightedColor = new Color(0.45f, 0.07f, 0.045f, 1f);
        colors.pressedColor = new Color(0.08f, 0.025f, 0.02f, 1f);
        button.colors = colors;
        button.onClick.AddListener(action);

        TextMeshProUGUI buttonText = CreateText("Label", buttonObject.transform, label, 17,
            Vector2.zero, Vector2.one, TextAlignmentOptions.Center, new Color(0.94f, 0.92f, 0.88f));
        buttonText.margin = new Vector4(8f, 4f, 8f, 4f);
        return button;
    }

    private TMP_InputField CreateInput(string objectName, Transform parent, string placeholder,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject inputObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        inputObject.transform.SetParent(parent, false);
        RectTransform rect = inputObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        inputObject.GetComponent<Image>().color = new Color(0.035f, 0.045f, 0.055f, 0.98f);

        TextMeshProUGUI valueText = CreateText("Value", inputObject.transform, "", 18,
            new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f),
            TextAlignmentOptions.Left, Color.white);
        TextMeshProUGUI placeholderText = CreateText("Placeholder", inputObject.transform, placeholder, 15,
            new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f),
            TextAlignmentOptions.Left, new Color(0.48f, 0.52f, 0.56f));

        TMP_InputField input = inputObject.GetComponent<TMP_InputField>();
        input.textComponent = valueText;
        input.placeholder = placeholderText;
        input.targetGraphic = inputObject.GetComponent<Image>();
        input.characterLimit = objectName == "Join code" ? 8 : 20;
        input.contentType = objectName == "Join code"
            ? TMP_InputField.ContentType.Alphanumeric
            : TMP_InputField.ContentType.Standard;
        return input;
    }
}
