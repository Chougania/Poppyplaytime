using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    public Slider sensitivitySlider;
    public TMP_Dropdown qualityDropdown;
    public TMP_Dropdown resolutionDropdown;
    public Toggle vSyncToggle;
    public Toggle fullScreenToggle;

    public RigidboyPlayerController playerController;

    private const string SensitivityKey = "Sensitivity";
    private const string QualityKey = "QualityLevel";
    private const string VSyncKey = "VSync";
    private const string FullScreenKey = "FullScreen";
    private const string ResolutionKey = "Resolution";

    private const string FOVKey = "FOV";
    public Slider fovSlider;
    public Camera playerCamera;

    public Slider renderScaleSlider;

    private const string RenderScaleKey = "RenderScale";

    bool isLoading = true;


    Resolution[] resolutions; 

    public Animator animator;

    public bool open = false;

    public LaunchHand[] hands;

    public Animator playerAnimator;

    public GameObject Dragsource1;
    public GameObject Dragsource2;

    public MobileIcons mobileIcons;

    public WeaponDragSway ItemDrag;

    public RawImage renderImage;
    private RenderTexture renderTexture;

    public bool disablemouse = true;

    public GameObject wheel;

    private bool wheelclosing = false;
    private bool wheelopening = false;


    public Slider masterVolumeSlider;

    private const string MasterVolumeKey = "MasterVolume";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        ResumeGame();
        SetupQualityDropdown();
        SetupResolutionDropdown();

        fullScreenToggle.onValueChanged.AddListener(SetFullscreen);
        vSyncToggle.onValueChanged.AddListener(SetVSync);
        sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
        qualityDropdown.onValueChanged.AddListener(SetQuality);
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
        fovSlider.onValueChanged.AddListener(SetFOV);
        fovSlider.minValue = 60f;
        fovSlider.maxValue = 100f;
        renderScaleSlider.onValueChanged.AddListener(SetRenderScale);
        renderScaleSlider.minValue = 0.1f;
        renderScaleSlider.maxValue = 1.0f;
        masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        masterVolumeSlider.minValue = 0f;
        masterVolumeSlider.maxValue = 1f;

        LoadSettings();


    }

    public void PauseGame()
    {
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Escape))
        {
            open = !open;
            updateOpenStatus(open);
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            UnlockCursor();
            playerController.canLook = false;
            OpenWheel();
        }

        if (Input.GetKeyUp(KeyCode.Q))
        {
            if (!open && disablemouse)
                LockCursor();

            playerController.canLook = true;
            CloseWheel();
        }
    }

    public void OpenWheel()
    {
        wheel.SetActive(true);

        foreach (LaunchHand hand in hands)
        {
            if (hand != null)
                hand.enabled = false;
        }
    }
    public void CloseWheel()
    {
        wheel.SetActive(false);

        foreach (LaunchHand hand in hands)
        {
            if (hand != null)
                hand.enabled = true;
        }
    }




    public void ToggleOpen()
    {
        open = !open;
        updateOpenStatus(open);
    }

    void updateOpenStatus(bool state)
    {
        animator.SetBool("open", state);
        playerController.enabled = !state;
        ItemDrag.enabled = !state;

        if (playerAnimator != null)
        {
            playerAnimator.enabled = !state;
        }

        foreach (LaunchHand hand in hands)
        {
            if (hand != null)
                hand.enabled = !state;
        }

        if (state)
        {


            UnlockCursor();
            playerController.StopFootsteps();
            Dragsource1.SetActive(false);
            Dragsource2.SetActive(false);
        }
        else
        {
            if (disablemouse)
            {
                LockCursor();
            }
            ResumeGame();

        }
    }

    void SetupQualityDropdown()
    {
        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
    }


    public void SetMasterVolume(float value)
    {
        AudioListener.volume = value;

        if (!isLoading)
        {
            PlayerPrefs.SetFloat(MasterVolumeKey, value);
        }
    }

    void SetupResolutionDropdown()
    {
        resolutions = Screen.resolutions;

        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();

        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    public void SetVSync(bool enabled)
    {
        QualitySettings.vSyncCount = enabled ? 1 : 0;
        PlayerPrefs.SetInt(VSyncKey, enabled ? 1 : 0);
    }

    public void SetFOV(float value)
    {
        if (playerCamera != null)
        {
            playerCamera.fieldOfView = value;
        }

        if (!isLoading)
        {
            PlayerPrefs.SetFloat(FOVKey, value);
        }
    }

    public void SetFullscreen(bool enabled)
    {
        Screen.fullScreen = enabled;
        PlayerPrefs.SetInt(FullScreenKey, enabled ? 1 : 0);
    }

    public void SetRenderScale(float scale)
    {
        if (playerCamera == null) return;

        int width = Mathf.RoundToInt(Screen.width * scale);
        int height = Mathf.RoundToInt(Screen.height * scale);

        if (renderTexture != null)
        {
            renderTexture.Release();
        }

        renderTexture = new RenderTexture(width, height, 24);

        //playerCamera.targetTexture = renderTexture;

        if (renderImage != null)
        {
            renderImage.texture = renderTexture;
        }

        if (!isLoading)
        {
            PlayerPrefs.SetFloat(RenderScaleKey, scale);
        }

        Debug.Log($"Render Scale: {scale} ({width}x{height})");
    }

    public void SetSensitivity(float value)
    {
        if (playerController != null)
        {
            playerController.lookSpeedX = value;
            playerController.lookSpeedY = value;
        }

        PlayerPrefs.SetFloat(SensitivityKey, value);
    }

    public void SetQuality(int index)
    {
        QualitySettings.SetQualityLevel(index);
        PlayerPrefs.SetInt(QualityKey, index);
    }

    public void SetResolution(int index)
    {
        Resolution res = resolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
        PlayerPrefs.SetInt(ResolutionKey, index);
    }

    void LoadSettings()
    {
        float savedSensitivity = PlayerPrefs.GetFloat(SensitivityKey, 1.5f);
        sensitivitySlider.value = savedSensitivity;
        SetSensitivity(savedSensitivity);

        bool hasSavedQuality = PlayerPrefs.HasKey(QualityKey);

        int savedQuality;

        if (Application.isMobilePlatform && !hasSavedQuality)
        {
            savedQuality = 0;
            PlayerPrefs.SetInt(QualityKey, 0);
        }
        else
        {
            savedQuality = PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel());
        }

        qualityDropdown.value = savedQuality;
        SetQuality(savedQuality);

        int savedVSync = PlayerPrefs.GetInt(VSyncKey, 0);
        vSyncToggle.isOn = savedVSync == 1;
        SetVSync(vSyncToggle.isOn);

        if (!Application.isMobilePlatform)
        {
            int savedFullScreen = PlayerPrefs.GetInt(FullScreenKey, 1);
            fullScreenToggle.isOn = savedFullScreen == 1;
            SetFullscreen(fullScreenToggle.isOn);
        }
        else
        {
            fullScreenToggle.gameObject.SetActive(false);
            resolutionDropdown.gameObject.SetActive(false);
        }

        if (!Application.isMobilePlatform)
        {
            int savedResolution = PlayerPrefs.GetInt(ResolutionKey, resolutionDropdown.value);
            resolutionDropdown.value = savedResolution;
            SetResolution(savedResolution);
        }

        float savedFOV = PlayerPrefs.GetFloat(FOVKey, 81f);
        fovSlider.value = savedFOV;
        SetFOV(savedFOV);

        float savedScale = PlayerPrefs.GetFloat(RenderScaleKey, 1f);
        renderScaleSlider.value = savedScale;
        SetRenderScale(savedScale);

        float savedVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        masterVolumeSlider.value = savedVolume;
        SetMasterVolume(savedVolume);

        isLoading = false;
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void LockCursor()
    {
        if (disablemouse)
        {
            if (mobileIcons.isMobile == false)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }


    }

    public void SetActiveCamera(Camera newCam)
    {
        if (newCam == null) return;

        if (playerCamera != null)
        {
            playerCamera.targetTexture = null;
            playerCamera.enabled = false;
        }

        playerCamera = newCam;

        playerCamera.enabled = true;
        playerCamera.targetTexture = renderTexture;
    }
}