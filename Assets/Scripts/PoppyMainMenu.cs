using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Cinematic runtime front-end: static hero composition, camera-driven shop and monster
/// showroom, horizontal-only mouse rotation, and Enter-to-play.
/// </summary>
public class PoppyMainMenu : MonoBehaviour
{
    [Header("Scene references")]
    public Camera menuCamera;
    public Transform characterPreview;
    public Transform huggyPreview;
    public ConnectionManafer connectionManager;

    [Header("Factory background")]
    public bool buildCorridorAtRuntime = true;
    [Min(20f)] public float corridorLength = 42f;
    public Color corridorTint = new Color(0.045f, 0.052f, 0.06f);
    public Color warningLightColor = new Color(1f, 0.045f, 0.018f);

    [Header("Camera transitions")]
    [Min(0.1f)] public float cameraTransitionDuration = 0.75f;
    [Min(0f)] public float showroomAutoRotateSpeed = 12f;
    [Min(0f)] public float mouseRotateSensitivity = 0.22f;

    private enum MenuView { Main, Shop, Monsters }
    private MenuView currentView = MenuView.Main;
    private Canvas menuCanvas;
    private TMP_InputField playerNameInput;
    private TMP_InputField joinCodeInput;
    private TextMeshProUGUI walletLabel;
    private TextMeshProUGUI statusLabel;
    private TextMeshProUGUI shopStatusLabel;
    private TextMeshProUGUI monsterNameLabel;
    private TextMeshProUGUI monsterPriceLabel;
    private TextMeshProUGUI monsterStatusLabel;
    private TextMeshProUGUI shopCreditsLabel;
    private GameObject mainPanel;
    private GameObject shopPanel;
    private GameObject monsterPanel;
    private Transform environmentRoot;
    private Transform playerStand;
    private Transform[] monsterBoxes = new Transform[3];
    private Material concreteMaterial;
    private Material darkMetalMaterial;
    private Material redMaterial;
    private Material blueGlowMaterial;
    private Material glassMaterial;
    private Vector3 mainCameraPosition;
    private Quaternion mainCameraRotation;
    private Vector3 shopCameraPosition;
    private Quaternion shopCameraRotation;
    private Vector3 monsterCameraPosition;
    private Quaternion monsterCameraRotation;
    private Coroutine cameraTransition;
    private bool wasConnectedToSession;
    private bool isTransitioning;
    private int selectedMonsterIndex;
    private float dragStartX;
    private float dragYaw;
    private float autoYaw;

    private void Start()
    {
        if (menuCamera == null) menuCamera = Camera.main;
        if (connectionManager == null) connectionManager = FindObjectOfType<ConnectionManafer>();

        if (connectionManager != null)
        {
            if (connectionManager.canvas1 != null) connectionManager.canvas1.SetActive(false);
            if (connectionManager.canvas2 != null) connectionManager.canvas2.SetActive(false);
            if (playerNameInput != null) connectionManager.nameInputField = playerNameInput;
        }

        if (buildCorridorAtRuntime) BuildFactoryShowroom();
        BuildInterface();
        CacheCameraShots();
        PrepareCharacterPreview();
        PrepareMonsterPreview();

        PoppyUIInputGate.Clear();
        PoppyUIInputGate.SetMenuOpen(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ApplyPlayerSkin();
        ApplyMonsterSkin();
    }

    private void Update()
    {
        bool connected = connectionManager != null && connectionManager.networkManager != null &&
            connectionManager.networkManager.IsListening;
        if (connected)
        {
            wasConnectedToSession = true;
            if (menuCanvas != null) menuCanvas.gameObject.SetActive(false);
            PoppyUIInputGate.Clear();
            if (environmentRoot != null) environmentRoot.gameObject.SetActive(false);
            if (characterPreview != null) characterPreview.gameObject.SetActive(false);
            if (huggyPreview != null) huggyPreview.gameObject.SetActive(false);
            if (menuCamera != null) menuCamera.enabled = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            return;
        }

        if (wasConnectedToSession)
        {
            wasConnectedToSession = false;
            if (menuCanvas != null) menuCanvas.gameObject.SetActive(true);
            if (environmentRoot != null) environmentRoot.gameObject.SetActive(true);
            if (characterPreview != null) characterPreview.gameObject.SetActive(true);
            PoppyUIInputGate.Clear();
            PoppyUIInputGate.SetMenuOpen(true);
            currentView = MenuView.Main;
            RestoreMainViewImmediate();
            if (menuCamera != null) menuCamera.enabled = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (walletLabel != null)
            walletLabel.text = "CREDITS  " + PoppyShop.Credits + "       ★  TICKETS  " + PoppyShop.Tickets;

        if (currentView == MenuView.Shop)
            UpdatePlayerTurntable();
        else if (currentView == MenuView.Monsters)
            UpdateMonsterTurntable();

        if (currentView == MenuView.Main && !IsInputFieldFocused() &&
            (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            StartSoloAI();

        if (Input.GetKeyDown(KeyCode.Escape) && currentView != MenuView.Main)
            ReturnToMainView();
    }

    private void BuildFactoryShowroom()
    {
        if (menuCamera == null)
        {
            Debug.LogWarning("PoppyMainMenu: no camera found; UI will still be created.", this);
            return;
        }

        GameObject root = new GameObject("POPPY_CINEMATIC_MENU_ENVIRONMENT");
        environmentRoot = root.transform;
        environmentRoot.position = transform.position;

        concreteMaterial = CreateMaterial("Menu concrete", corridorTint);
        darkMetalMaterial = CreateMaterial("Menu dark metal", new Color(0.018f, 0.025f, 0.032f));
        redMaterial = CreateMaterial("Menu warning red", new Color(0.62f, 0.035f, 0.018f));
        blueGlowMaterial = CreateMaterial("Menu blue glow", new Color(0.04f, 0.42f, 0.78f));
        glassMaterial = CreateMaterial("Menu glass tint", new Color(0.08f, 0.16f, 0.2f, 0.24f));

        Block("Factory floor", new Vector3(0f, -0.2f, corridorLength * 0.5f), new Vector3(16f, 0.4f, corridorLength), darkMetalMaterial);
        Block("Factory back wall", new Vector3(0f, 4f, corridorLength), new Vector3(16f, 8f, 0.5f), concreteMaterial);
        Block("Factory left wall", new Vector3(-8f, 4f, corridorLength * 0.5f), new Vector3(0.5f, 8f, corridorLength), concreteMaterial);
        Block("Factory right wall", new Vector3(8f, 4f, corridorLength * 0.5f), new Vector3(0.5f, 8f, corridorLength), concreteMaterial);
        Block("Ceiling", new Vector3(0f, 8f, corridorLength * 0.5f), new Vector3(16f, 0.35f, corridorLength), darkMetalMaterial);

        for (int i = 0; i < 8; i++)
        {
            float z = 2f + i * 5f;
            Block("Wall support L " + i, new Vector3(-7.5f, 4f, z), new Vector3(0.28f, 8f, 0.35f), darkMetalMaterial);
            Block("Wall support R " + i, new Vector3(7.5f, 4f, z), new Vector3(0.28f, 8f, 0.35f), darkMetalMaterial);
            Block("Ceiling light " + i, new Vector3(0f, 7.72f, z), new Vector3(2.2f, 0.12f, 0.7f), redMaterial);
            Light light = NewPointLight("Factory flicker " + i, new Vector3(0f, 7.1f, z),
                i % 3 == 0 ? warningLightColor : new Color(0.48f, 0.68f, 0.82f),
                i % 3 == 0 ? 2.8f : 1.65f, 12f);
            StartCoroutine(Flicker(light, i));
        }

        BuildPlayerTurntable();
        BuildMonsterShowroom();
        BuildFactoryDetails();

        menuCamera.fieldOfView = 48f;
        menuCamera.transform.position = environmentRoot.position + new Vector3(0f, 2.35f, -9.5f);
        menuCamera.transform.rotation = Quaternion.LookRotation(
            (environmentRoot.position + new Vector3(0f, 2.2f, 9f)) - menuCamera.transform.position);
    }

    private void BuildPlayerTurntable()
    {
        GameObject stand = new GameObject("PLAYER_SHOWROOM_TURNTABLE");
        stand.transform.SetParent(environmentRoot, false);
        stand.transform.localPosition = new Vector3(3.4f, 0f, 3.4f);
        playerStand = stand.transform;
        Cylinder("Player display base", playerStand, new Vector3(0f, 0.18f, 0f),
            new Vector3(1.35f, 0.18f, 1.35f), darkMetalMaterial);
        Cylinder("Player display ring", playerStand, new Vector3(0f, 0.31f, 0f),
            new Vector3(1.15f, 0.035f, 1.15f), blueGlowMaterial);
        PointLightChild("Player display light", playerStand, new Vector3(0f, 2.2f, 0f),
            new Color(0.2f, 0.55f, 1f), 2.1f, 5f);
    }

    private void BuildMonsterShowroom()
    {
        Vector3[] positions =
        {
            new Vector3(-5.2f, 0f, 17f),
            new Vector3(0f, 0f, 17f),
            new Vector3(5.2f, 0f, 17f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject box = new GameObject("MONSTER_CONTAINMENT_BOX_" + (i + 1));
            box.transform.SetParent(environmentRoot, false);
            box.transform.localPosition = positions[i];
            monsterBoxes[i] = box.transform;
            BuildContainmentBox(box.transform, i);
        }
    }

    private void BuildContainmentBox(Transform box, int index)
    {
        Material accent = index == 1 ? redMaterial : blueGlowMaterial;
        BlockChild("Containment floor", box, new Vector3(0f, 0.12f, 0f), new Vector3(3.1f, 0.24f, 3.1f), darkMetalMaterial);
        BlockChild("Containment back", box, new Vector3(0f, 2.1f, 1.42f), new Vector3(3.1f, 4f, 0.16f), darkMetalMaterial);
        BlockChild("Containment left", box, new Vector3(-1.48f, 2.1f, 0f), new Vector3(0.16f, 4f, 3f), darkMetalMaterial);
        BlockChild("Containment right", box, new Vector3(1.48f, 2.1f, 0f), new Vector3(0.16f, 4f, 3f), darkMetalMaterial);
        BlockChild("Containment top", box, new Vector3(0f, 4.08f, 0f), new Vector3(3.1f, 0.16f, 3.1f), darkMetalMaterial);
        BlockChild("Containment warning strip", box, new Vector3(0f, 3.86f, -1.48f), new Vector3(2.8f, 0.1f, 0.1f), accent);
        BlockChild("Containment glass", box, new Vector3(0f, 2f, -1.45f), new Vector3(2.85f, 3.7f, 0.035f), glassMaterial);
        CylinderChild("Containment platform", box, new Vector3(0f, 0.42f, 0f), new Vector3(0.95f, 0.12f, 0.95f), accent);
        PointLightChild("Containment spot", box, new Vector3(0f, 3.65f, 0f), accent.color, 2.5f, 6f);
    }

    private void BuildFactoryDetails()
    {
        Block("Overhead gantry", new Vector3(0f, 6.4f, 11f), new Vector3(14f, 0.35f, 0.45f), darkMetalMaterial);
        Block("PLAYTIME sign backing", new Vector3(-4.7f, 5.2f, 1.8f), new Vector3(4.8f, 1.05f, 0.22f), redMaterial);
        GameObject sign = new GameObject("PLAYTIME CO SIGN");
        sign.transform.SetParent(environmentRoot, false);
        sign.transform.localPosition = new Vector3(-4.7f, 5.2f, 1.65f);
        TextMesh text = sign.AddComponent<TextMesh>();
        text.text = "PLAYTIME CO.";
        text.fontSize = 72;
        text.characterSize = 0.025f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = new Color(1f, 0.84f, 0.7f);
    }

    private void CacheCameraShots()
    {
        if (menuCamera == null) return;
        mainCameraPosition = menuCamera.transform.position;
        mainCameraRotation = menuCamera.transform.rotation;

        Vector3 playerTarget = playerStand != null
            ? playerStand.position + Vector3.up * 1.6f
            : transform.position + new Vector3(3f, 1.5f, 3f);
        shopCameraPosition = playerTarget + new Vector3(0f, 0.65f, -4.4f);
        shopCameraRotation = Quaternion.LookRotation(playerTarget - shopCameraPosition);

        Vector3 monsterTarget = environmentRoot != null
            ? environmentRoot.position + new Vector3(0f, 2f, 17f)
            : transform.position + new Vector3(0f, 2f, 17f);
        monsterCameraPosition = monsterTarget + new Vector3(0f, 1.1f, -9.5f);
        monsterCameraRotation = Quaternion.LookRotation(monsterTarget - monsterCameraPosition);
    }

    private void PrepareCharacterPreview()
    {
        if (characterPreview == null) return;
        characterPreview.gameObject.SetActive(true);
        characterPreview.position = playerStand != null
            ? playerStand.position + new Vector3(0f, 0.42f, 0f)
            : transform.position + new Vector3(3f, 0f, 3f);
        characterPreview.rotation = Quaternion.Euler(0f, 180f, 0f);
        PausePreviewAnimator(characterPreview);
        DisablePreviewPhysics(characterPreview);
    }

    private void PrepareMonsterPreview()
    {
        if (huggyPreview == null) return;
        huggyPreview.gameObject.SetActive(false);
        selectedMonsterIndex = PoppyMonsterShop.SelectedSkin;
        if (monsterBoxes[1] != null)
        {
            huggyPreview.SetParent(monsterBoxes[1], true);
            huggyPreview.localPosition = new Vector3(0f, 0.45f, 0.15f);
            huggyPreview.localRotation = Quaternion.Euler(0f, 180f, 0f);
        }
        PausePreviewAnimator(huggyPreview);
        DisablePreviewBehaviours(huggyPreview);
        DisablePreviewPhysics(huggyPreview);
    }

    private void PausePreviewAnimator(Transform root)
    {
        if (root == null) return;
        Animator[] animators = root.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            animators[i].speed = 0f;
            animators[i].applyRootMotion = false;
        }
    }

    private void DisablePreviewBehaviours(Transform root)
    {
        if (root == null) return;
        Behaviour[] behaviours = root.GetComponentsInChildren<Behaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (!(behaviours[i] is Animator))
                behaviours[i].enabled = false;
        }
    }

    private void DisablePreviewPhysics(Transform root)
    {
        if (root == null) return;
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = false;
        Rigidbody[] bodies = root.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < bodies.Length; i++)
        {
            bodies[i].isKinematic = true;
            bodies[i].useGravity = false;
            bodies[i].velocity = Vector3.zero;
            bodies[i].angularVelocity = Vector3.zero;
        }
        CharacterController[] controllers = root.GetComponentsInChildren<CharacterController>(true);
        for (int i = 0; i < controllers.Length; i++) controllers[i].enabled = false;
    }

    private void UpdatePlayerTurntable()
    {
        if (characterPreview == null || playerStand == null || isTransitioning) return;
        if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
        {
            dragStartX = Input.mousePosition.x;
            dragYaw = characterPreview.eulerAngles.y;
        }
        if (Input.GetMouseButton(0) && !IsPointerOverUI())
        {
            float delta = Input.mousePosition.x - dragStartX;
            characterPreview.rotation = Quaternion.Euler(0f, dragYaw + delta * mouseRotateSensitivity, 0f);
        }
        else if (!Input.GetMouseButton(0))
        {
            characterPreview.Rotate(Vector3.up, showroomAutoRotateSpeed * Time.unscaledDeltaTime, Space.World);
        }
    }

    private void UpdateMonsterTurntable()
    {
        if (huggyPreview == null || !huggyPreview.gameObject.activeSelf || isTransitioning) return;
        if (Input.GetMouseButtonDown(0) && !IsPointerOverUI())
        {
            dragStartX = Input.mousePosition.x;
            dragYaw = huggyPreview.eulerAngles.y;
        }
        if (Input.GetMouseButton(0) && !IsPointerOverUI())
        {
            float delta = Input.mousePosition.x - dragStartX;
            huggyPreview.rotation = Quaternion.Euler(0f, dragYaw + delta * mouseRotateSensitivity, 0f);
        }
        else if (!Input.GetMouseButton(0))
        {
            huggyPreview.Rotate(Vector3.up, showroomAutoRotateSpeed * Time.unscaledDeltaTime, Space.World);
        }
    }

    private void BuildInterface()
    {
        GameObject canvasObject = new GameObject("PoppyCinematicMainMenuCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        menuCanvas = canvasObject.GetComponent<Canvas>();
        menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        EnsureEventSystem();
        walletLabel = CreateText("Wallet", menuCanvas.transform, "", 18,
            new Vector2(0.72f, 0.925f), new Vector2(0.98f, 0.98f), TextAlignmentOptions.Right, new Color(1f, 0.78f, 0.3f));
        CreateText("Creator credit", menuCanvas.transform, "CREATED BY CHOUGAN2", 14,
            new Vector2(0.035f, 0.93f), new Vector2(0.34f, 0.975f), TextAlignmentOptions.Left, new Color(1f, 0.48f, 0.24f));

        mainPanel = CreatePanel("Main menu panel", menuCanvas.transform,
            new Vector2(0.035f, 0.12f), new Vector2(0.43f, 0.89f), new Color(0.008f, 0.014f, 0.021f, 0.88f));
        CreateText("Main title", mainPanel.transform, "PROJECT\nPLAYTIME", 52,
            new Vector2(0.08f, 0.69f), new Vector2(0.92f, 0.94f), TextAlignmentOptions.Left, Color.white);
        CreateText("Main subtitle", mainPanel.transform, "REMADE  /  MULTIPLAYER HORROR", 15,
            new Vector2(0.08f, 0.64f), new Vector2(0.92f, 0.7f), TextAlignmentOptions.Left, new Color(1f, 0.27f, 0.18f));

        playerNameInput = CreateInput("Player name", mainPanel.transform, "PLAYER NAME",
            new Vector2(0.08f, 0.55f), new Vector2(0.92f, 0.62f));
        joinCodeInput = CreateInput("Join code", mainPanel.transform, "RELAY CODE (OPTIONAL)",
            new Vector2(0.08f, 0.46f), new Vector2(0.92f, 0.53f));

        CreateButton("Play", mainPanel.transform, "▶   PLAY  /  SOLO AI     [ENTER]",
            new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.435f), StartSoloAI, true);
        CreateButton("Host", mainPanel.transform, "CREATE ONLINE ROOM",
            new Vector2(0.08f, 0.245f), new Vector2(0.50f, 0.325f), HostSession, false);
        CreateButton("Join", mainPanel.transform, "JOIN ROOM",
            new Vector2(0.54f, 0.245f), new Vector2(0.92f, 0.325f), JoinSession, false);
        CreateButton("Monsters", mainPanel.transform, "MONSTERS",
            new Vector2(0.08f, 0.135f), new Vector2(0.50f, 0.22f), OpenMonsterSelection, false);
        CreateButton("Shop", mainPanel.transform, "SHOP",
            new Vector2(0.54f, 0.135f), new Vector2(0.92f, 0.22f), OpenShop, false);
        statusLabel = CreateText("Menu status", mainPanel.transform, "PRESS ENTER TO START  •  ESC TO RETURN",
            12, new Vector2(0.08f, 0.035f), new Vector2(0.92f, 0.105f), TextAlignmentOptions.Left, new Color(0.58f, 0.64f, 0.7f));

        BuildShopPanel();
        BuildMonsterPanel();
        if (connectionManager != null)
        {
            connectionManager.nameInputField = playerNameInput;
            connectionManager.codeInputField = joinCodeInput;
        }
    }

    private void BuildShopPanel()
    {
        shopPanel = CreatePanel("Player shop panel", menuCanvas.transform,
            new Vector2(0.035f, 0.12f), new Vector2(0.43f, 0.89f), new Color(0.008f, 0.014f, 0.021f, 0.94f));
        CreateText("Shop title", shopPanel.transform, "PLAYER\nCUSTOMIZATION", 38,
            new Vector2(0.08f, 0.76f), new Vector2(0.92f, 0.96f), TextAlignmentOptions.Left, Color.white);
        shopCreditsLabel = CreateText("Shop wallet", shopPanel.transform, "", 16,
            new Vector2(0.08f, 0.70f), new Vector2(0.92f, 0.77f), TextAlignmentOptions.Left, new Color(1f, 0.78f, 0.3f));
        CreateText("Shop instruction", shopPanel.transform, "DRAG LEFT / RIGHT TO ROTATE YOUR TOY",
            13, new Vector2(0.08f, 0.64f), new Vector2(0.92f, 0.69f), TextAlignmentOptions.Left, new Color(0.55f, 0.72f, 0.9f));

        for (int i = 0; i < PoppyShop.ColorCount; i++)
        {
            int index = i;
            int column = i % 2;
            int row = i / 2;
            float y = 0.55f - row * 0.105f;
            CreateButton("ShopColor" + i, shopPanel.transform, GetPlayerColorLabel(index),
                new Vector2(0.08f + column * 0.43f, y), new Vector2(0.48f + column * 0.43f, y + 0.085f),
                () => BuyOrEquipColor(index), false);
        }
        shopStatusLabel = CreateText("Shop status", shopPanel.transform, "SELECT A COLOR TO EQUIP",
            12, new Vector2(0.08f, 0.13f), new Vector2(0.92f, 0.19f), TextAlignmentOptions.Left, new Color(0.7f, 0.76f, 0.82f));
        CreateButton("Shop back", shopPanel.transform, "← BACK",
            new Vector2(0.08f, 0.035f), new Vector2(0.92f, 0.115f), ReturnToMainView, false);
        shopPanel.SetActive(false);
    }

    private void BuildMonsterPanel()
    {
        monsterPanel = CreatePanel("Monster selection panel", menuCanvas.transform,
            new Vector2(0.035f, 0.12f), new Vector2(0.43f, 0.89f), new Color(0.008f, 0.014f, 0.021f, 0.94f));
        CreateText("Monster title", monsterPanel.transform, "MONSTER\nCONTAINMENT", 38,
            new Vector2(0.08f, 0.76f), new Vector2(0.92f, 0.96f), TextAlignmentOptions.Left, Color.white);
        CreateText("Monster hint", monsterPanel.transform, "CHOOSE YOUR EXPERIMENT",
            14, new Vector2(0.08f, 0.70f), new Vector2(0.92f, 0.76f), TextAlignmentOptions.Left, new Color(1f, 0.3f, 0.2f));
        monsterNameLabel = CreateText("Monster name", monsterPanel.transform, "", 24,
            new Vector2(0.08f, 0.57f), new Vector2(0.92f, 0.67f), TextAlignmentOptions.Left, Color.white);
        monsterPriceLabel = CreateText("Monster price", monsterPanel.transform, "", 15,
            new Vector2(0.08f, 0.51f), new Vector2(0.92f, 0.57f), TextAlignmentOptions.Left, new Color(1f, 0.78f, 0.3f));
        CreateButton("Previous monster", monsterPanel.transform, "◀ PREVIOUS",
            new Vector2(0.08f, 0.39f), new Vector2(0.48f, 0.48f), PreviousMonster, false);
        CreateButton("Next monster", monsterPanel.transform, "NEXT ▶",
            new Vector2(0.52f, 0.39f), new Vector2(0.92f, 0.48f), NextMonster, false);
        CreateButton("Select monster", monsterPanel.transform, "UNLOCK / SELECT",
            new Vector2(0.08f, 0.27f), new Vector2(0.92f, 0.365f), SelectMonster, true);
        monsterStatusLabel = CreateText("Monster status", monsterPanel.transform, "DRAG LEFT / RIGHT TO INSPECT",
            12, new Vector2(0.08f, 0.17f), new Vector2(0.92f, 0.23f), TextAlignmentOptions.Left, new Color(0.7f, 0.76f, 0.82f));
        CreateButton("Monster back", monsterPanel.transform, "← BACK",
            new Vector2(0.08f, 0.035f), new Vector2(0.92f, 0.12f), ReturnToMainView, false);
        monsterPanel.SetActive(false);
    }

    private void OpenShop()
    {
        if (currentView == MenuView.Shop || isTransitioning) return;
        currentView = MenuView.Shop;
        mainPanel.SetActive(false);
        monsterPanel.SetActive(false);
        shopPanel.SetActive(true);
        PoppyUIInputGate.SetShopOpen(true);
        PoppyUIInputGate.SetMenuOpen(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        RefreshShop();
        TransitionCamera(shopCameraPosition, shopCameraRotation);
    }

    private void OpenMonsterSelection()
    {
        if (currentView == MenuView.Monsters || isTransitioning) return;
        currentView = MenuView.Monsters;
        mainPanel.SetActive(false);
        shopPanel.SetActive(false);
        monsterPanel.SetActive(true);
        selectedMonsterIndex = PoppyMonsterShop.SelectedSkin;
        PoppyUIInputGate.SetShopOpen(true);
        PoppyUIInputGate.SetMenuOpen(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        RefreshMonsterSelection();
        ShowMonsterInBox();
        TransitionCamera(monsterCameraPosition, monsterCameraRotation);
    }

    private void ReturnToMainView()
    {
        if (currentView == MenuView.Main || isTransitioning) return;
        currentView = MenuView.Main;
        shopPanel.SetActive(false);
        monsterPanel.SetActive(false);
        mainPanel.SetActive(true);
        PoppyUIInputGate.SetShopOpen(false);
        PoppyUIInputGate.SetMenuOpen(true);
        if (huggyPreview != null) huggyPreview.gameObject.SetActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        TransitionCamera(mainCameraPosition, mainCameraRotation);
    }

    private void RestoreMainViewImmediate()
    {
        if (cameraTransition != null) StopCoroutine(cameraTransition);
        isTransitioning = false;
        if (menuCamera != null)
        {
            menuCamera.transform.position = mainCameraPosition;
            menuCamera.transform.rotation = mainCameraRotation;
        }
        if (mainPanel != null) mainPanel.SetActive(true);
        if (shopPanel != null) shopPanel.SetActive(false);
        if (monsterPanel != null) monsterPanel.SetActive(false);
        if (huggyPreview != null) huggyPreview.gameObject.SetActive(false);
        if (PoppyUIInputGate.IsBlocked) PoppyUIInputGate.Clear();
        PoppyUIInputGate.SetMenuOpen(true);
    }

    private void TransitionCamera(Vector3 targetPosition, Quaternion targetRotation)
    {
        if (menuCamera == null) return;
        if (cameraTransition != null) StopCoroutine(cameraTransition);
        cameraTransition = StartCoroutine(AnimateCamera(targetPosition, targetRotation));
    }

    private IEnumerator AnimateCamera(Vector3 targetPosition, Quaternion targetRotation)
    {
        isTransitioning = true;
        Vector3 startPosition = menuCamera.transform.position;
        Quaternion startRotation = menuCamera.transform.rotation;
        float elapsed = 0f;
        while (elapsed < cameraTransitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / cameraTransitionDuration);
            t = t * t * (3f - 2f * t);
            menuCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            menuCamera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }
        menuCamera.transform.position = targetPosition;
        menuCamera.transform.rotation = targetRotation;
        isTransitioning = false;
        cameraTransition = null;
    }

    private void PreviousMonster()
    {
        selectedMonsterIndex = (selectedMonsterIndex + PoppyMonsterShop.SkinCount - 1) % PoppyMonsterShop.SkinCount;
        RefreshMonsterSelection();
        ShowMonsterInBox();
    }

    private void NextMonster()
    {
        selectedMonsterIndex = (selectedMonsterIndex + 1) % PoppyMonsterShop.SkinCount;
        RefreshMonsterSelection();
        ShowMonsterInBox();
    }

    private void ShowMonsterInBox()
    {
        if (huggyPreview == null || monsterBoxes[1] == null) return;
        huggyPreview.gameObject.SetActive(true);
        huggyPreview.SetParent(monsterBoxes[1], true);
        huggyPreview.localPosition = new Vector3(0f, 0.48f, 0.05f);
        huggyPreview.localRotation = Quaternion.Euler(0f, 180f, 0f);
        ApplyMonsterSkin();
    }

    private void RefreshMonsterSelection()
    {
        if (monsterNameLabel != null) monsterNameLabel.text = PoppyMonsterShop.GetName(selectedMonsterIndex);
        bool unlocked = PoppyMonsterShop.IsUnlocked(selectedMonsterIndex);
        int ticketPrice = PoppyMonsterShop.GetTicketPrice(selectedMonsterIndex);
        int creditPrice = PoppyMonsterShop.GetPrice(selectedMonsterIndex);
        if (monsterPriceLabel != null)
            monsterPriceLabel.text = unlocked ? "OWNED  •  " + (PoppyMonsterShop.SelectedSkin == selectedMonsterIndex ? "EQUIPPED" : "READY TO SELECT")
                : (ticketPrice > 0 ? ticketPrice + " RARE TICKET(S)" : creditPrice + " CREDITS");
        if (monsterStatusLabel != null)
            monsterStatusLabel.text = "CREDITS " + PoppyShop.Credits + "   /   TICKETS " + PoppyShop.Tickets;
    }

    private void SelectMonster()
    {
        bool success = PoppyMonsterShop.BuyOrSelect(selectedMonsterIndex);
        if (monsterStatusLabel != null)
            monsterStatusLabel.text = success ? "SELECTED: " + PoppyMonsterShop.GetName(selectedMonsterIndex)
                : (PoppyMonsterShop.GetTicketPrice(selectedMonsterIndex) > 0 ? "NOT ENOUGH TICKETS" : "NOT ENOUGH CREDITS");
        RefreshMonsterSelection();
        ApplyMonsterSkin();
    }

    private void RefreshShop()
    {
        if (shopCreditsLabel != null)
            shopCreditsLabel.text = "CREDITS " + PoppyShop.Credits + "   /   TICKETS " + PoppyShop.Tickets;
        if (shopPanel == null) return;
        for (int i = 0; i < PoppyShop.ColorCount; i++)
        {
            Transform item = shopPanel.transform.Find("ShopColor" + i);
            if (item != null)
            {
                TMP_Text label = item.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = GetPlayerColorLabel(i);
            }
        }
    }

    private string GetPlayerColorLabel(int index)
    {
        if (PoppyShop.IsUnlocked(index))
            return (PoppyShop.SelectedColor == index ? "✓ " : "") + PoppyShop.GetColorName(index) + "  •  OWNED";
        return PoppyShop.GetColorName(index) + "  •  " + PoppyShop.GetPrice(index) + " C";
    }

    private void BuyOrEquipColor(int index)
    {
        bool success = PoppyShop.BuyOrSelect(index);
        if (shopStatusLabel != null)
            shopStatusLabel.text = success ? "EQUIPPED: " + PoppyShop.GetColorName(index) : "NOT ENOUGH CREDITS";
        RefreshShop();
        ApplyPlayerSkin();
    }

    private void ApplyPlayerSkin()
    {
        if (characterPreview != null)
            PoppyShop.ApplyColor(characterPreview.gameObject, PoppyShop.SelectedColor);
    }

    private void ApplyMonsterSkin()
    {
        if (huggyPreview != null)
            PoppyMonsterShop.Apply(huggyPreview.gameObject, selectedMonsterIndex);
    }

    private void StartSoloAI()
    {
        if (connectionManager == null)
        {
            Debug.LogError("PoppyMainMenu: ConnectionManafer is missing.", this);
            if (statusLabel != null) statusLabel.text = "NETWORK MANAGER NOT FOUND";
            return;
        }
        SavePlayerName();
        PlayerPrefs.SetInt("Poppy.EnemyMode", 1);
        PlayerPrefs.Save();
        if (statusLabel != null) statusLabel.text = "CONNECTING TO LOCAL MATCH...";
        PoppyUIInputGate.Clear();
        connectionManager.HostSoloOffline();
    }

    private void HostSession()
    {
        if (connectionManager == null) return;
        SavePlayerName();
        PlayerPrefs.SetInt("Poppy.EnemyMode", 0);
        PlayerPrefs.Save();
        if (statusLabel != null) statusLabel.text = "CREATING ONLINE ROOM...";
        PoppyUIInputGate.Clear();
        connectionManager.HostAServer();
    }

    private void JoinSession()
    {
        if (connectionManager == null) return;
        SavePlayerName();
        connectionManager.codeInputField = joinCodeInput;
        if (joinCodeInput == null || string.IsNullOrWhiteSpace(joinCodeInput.text))
            connectionManager.JoinRandomServer();
        else
            connectionManager.JoinWithCode();
    }

    private void SavePlayerName()
    {
        string value = playerNameInput != null ? playerNameInput.text.Trim() : "";
        if (string.IsNullOrEmpty(value)) value = "Player";
        if (value.Length > 20) value = value.Substring(0, 20);
        ConnectionManafer.PlayerName = value;
    }

    private bool IsInputFieldFocused()
    {
        return (playerNameInput != null && playerNameInput.isFocused) ||
               (joinCodeInput != null && joinCodeInput.isFocused);
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private void EnsureEventSystem()
    {
        EventSystem existing = FindObjectOfType<EventSystem>();
        if (existing == null)
        {
            GameObject root = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(root);
            return;
        }
        EventSystem[] all = FindObjectsOfType<EventSystem>();
        for (int i = 0; i < all.Length; i++)
            if (all[i] != existing) Destroy(all[i].gameObject);
    }

    private IEnumerator Flicker(Light light, int seed)
    {
        if (light == null) yield break;
        float baseIntensity = light.intensity;
        System.Random random = new System.Random(seed * 7919 + 31);
        while (light != null)
        {
            yield return new WaitForSecondsRealtime(1.5f + (float)random.NextDouble() * 3.5f);
            int count = random.Next(2, 5);
            for (int i = 0; i < count && light != null; i++)
            {
                light.enabled = false;
                yield return new WaitForSecondsRealtime(0.04f);
                if (light == null) yield break;
                light.enabled = true;
                light.intensity = baseIntensity * Random.Range(0.55f, 1.15f);
                yield return new WaitForSecondsRealtime(0.06f);
            }
            if (light != null) light.intensity = baseIntensity;
        }
    }

    private Light NewPointLight(string name, Vector3 position, Color color, float intensity, float range)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(environmentRoot, false);
        obj.transform.localPosition = position;
        Light light = obj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        return light;
    }

    private void PointLightChild(string name, Transform parent, Vector3 position, Color color, float intensity, float range)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        Light light = obj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
    }

    private Material CreateMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) return null;
        Material material = new Material(shader);
        material.name = name;
        material.color = color;
        return material;
    }

    private void Block(string name, Vector3 position, Vector3 scale, Material material)
    {
        CreatePrimitive(PrimitiveType.Cube, name, environmentRoot, position, scale, material);
    }

    private void BlockChild(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        CreatePrimitive(PrimitiveType.Cube, name, parent, position, scale, material);
    }

    private void Cylinder(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        CreatePrimitive(PrimitiveType.Cylinder, name, parent, position, scale, material);
    }

    private void CylinderChild(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        CreatePrimitive(PrimitiveType.Cylinder, name, parent, position, scale, material);
    }

    private void CreatePrimitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        obj.transform.localScale = scale;
        Collider collider = obj.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null && material != null) renderer.sharedMaterial = material;
    }

    private GameObject CreatePanel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = obj.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return obj;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, string value, float size,
        Vector2 min, Vector2 max, TextAlignmentOptions alignment, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null) text.font = TMP_Settings.defaultFontAsset;
        return text;
    }

    private Button CreateButton(string name, Transform parent, string label, Vector2 min, Vector2 max,
        UnityEngine.Events.UnityAction action, bool primary)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = obj.GetComponent<Image>();
        image.color = primary ? new Color(0.62f, 0.075f, 0.045f, 0.98f) : new Color(0.07f, 0.105f, 0.13f, 0.97f);
        Button button = obj.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = primary ? new Color(0.78f, 0.11f, 0.055f) : new Color(0.11f, 0.16f, 0.19f);
        colors.highlightedColor = new Color(0.92f, 0.23f, 0.1f);
        colors.pressedColor = new Color(0.035f, 0.045f, 0.055f);
        button.colors = colors;
        button.onClick.AddListener(action);
        TextMeshProUGUI text = CreateText("Label", obj.transform, label, primary ? 19 : 15,
            Vector2.zero, Vector2.one, TextAlignmentOptions.Center, new Color(0.96f, 0.96f, 0.94f));
        text.margin = new Vector4(8f, 3f, 8f, 3f);
        return button;
    }

    private TMP_InputField CreateInput(string name, Transform parent, string placeholder, Vector2 min, Vector2 max)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image background = obj.GetComponent<Image>();
        background.color = new Color(0.025f, 0.035f, 0.045f, 0.98f);
        TMP_InputField input = obj.GetComponent<TMP_InputField>();
        TextMeshProUGUI valueText = CreateText("Value", obj.transform, "", 17,
            new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f), TextAlignmentOptions.Left, Color.white);
        TextMeshProUGUI placeholderText = CreateText("Placeholder", obj.transform, placeholder, 14,
            new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f), TextAlignmentOptions.Left, new Color(0.48f, 0.55f, 0.62f));
        input.textComponent = valueText;
        input.placeholder = placeholderText;
        input.targetGraphic = background;
        input.characterLimit = name == "Join code" ? 8 : 20;
        input.contentType = name == "Join code" ? TMP_InputField.ContentType.Alphanumeric : TMP_InputField.ContentType.Standard;
        return input;
    }

    private void QuitGame()
    {
        Application.Quit();
    }

    private void OnDestroy()
    {
        if (PoppyUIInputGate.IsBlocked) PoppyUIInputGate.Clear();
        if (menuCamera != null && !wasConnectedToSession)
        {
            AudioListener listener = menuCamera.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = true;
        }
    }
}
