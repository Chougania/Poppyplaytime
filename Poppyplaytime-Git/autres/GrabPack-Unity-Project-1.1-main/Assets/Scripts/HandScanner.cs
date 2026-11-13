using UnityEngine;

[RequireComponent(typeof(PowerActivator))]
public class HandScanner : MonoBehaviour
{
    public string handToDetect = "Hand_Blue";

    public MeshRenderer screen;
    public Material screenIdle;
    public Material screenScanning;
    public Material greenBackground;

    public MeshRenderer scanningLabel;
    public Material ready;
    public Material scanning;
    public Material verified;

    public MeshRenderer handprint;
    public Material handprintMaterial;
    public Material smile;

    public Light scannerLight;
    public Color scannerColour;
    public Color green;

    public float minOffset = 0f;
    public float maxOffset = 1f;
    public float speed = 1f;
    public float scanDuration;

    public GameObject scanningAudio;

    private bool hasStarted;
    private bool scanned;
    private float scanningDuration;

    private PowerActivator powerActivator;

    void Awake()
    {
        powerActivator = GetComponent<PowerActivator>();
    }

    void OnEnable()
    {
        ResetScanner();
        powerActivator = GetComponent<PowerActivator>();


    }

    void OnDisable()
    {
        if (scanningAudio != null)
            scanningAudio.SetActive(false);
    }

    void Update()
    {
        ScrollMaterial(ready);

        if (!scanned)
        {
            Transform child = transform.Find(handToDetect);

            if (child != null)
            {
                if (!hasStarted)
                    StartScanning();
            }
            else
            {
                ResetIdleState();
            }

            if (hasStarted)
            {
                scanningDuration += Time.deltaTime;

                ScrollMaterial(scanning);

                float offset = Mathf.Lerp(minOffset, maxOffset, Mathf.PingPong(Time.time * speed, 1f));
                screenScanning.mainTextureOffset =
                    new Vector2(screenScanning.mainTextureOffset.x, offset);

                if (scanningDuration > scanDuration)
                    CompleteScan();
            }
        }
        else
        {
            ScrollMaterial(verified);
        }
    }

    void StartScanning()
    {
        scanningDuration = 0f;

        if (scanDuration <= 0f)
            scanDuration = 1f;

        hasStarted = true;

        if (scanningAudio != null)
            scanningAudio.SetActive(true);

        scanningLabel.material = scanning;
        screen.material = screenScanning;
    }

    void CompleteScan()
    {
        scanned = true;

        handprint.material = smile;
        scannerLight.color = green;

        screen.material = greenBackground;
        scanningLabel.material = verified;
        hasStarted = false;

        if (scanningAudio != null)
            scanningAudio.SetActive(false);

        if (powerActivator != null)
            powerActivator.Activate();
    }

    void ResetScanner()
    {
        scanned = false;
        hasStarted = false;
        scanningDuration = 0f;

        scannerLight.color = scannerColour;

        if (screen != null)
            screen.material = screenIdle;

        if (scanningLabel != null)
            scanningLabel.material = ready;
    }

    void ResetIdleState()
    {
        screen.material = screenIdle;
        scanningLabel.material = ready;
        hasStarted = false;

        if (scanningAudio != null)
            scanningAudio.SetActive(false);
    }

    void ScrollMaterial(Material mat)
    {
        if (mat == null) return;

        Vector2 offset = mat.mainTextureOffset;
        offset.x += speed * 0.5f * Time.deltaTime;
        mat.mainTextureOffset = offset;
    }
}