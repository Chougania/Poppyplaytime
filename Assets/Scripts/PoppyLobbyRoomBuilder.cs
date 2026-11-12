using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Creates a new Playtime factory waiting room around the configured lobby spawn.
/// Uses built-in meshes/materials so it works without downloading extra assets.
/// </summary>
public class PoppyLobbyRoomBuilder : MonoBehaviour
{
    public Transform anchor;
    public Camera lobbyCamera;
    public Vector3 cameraOffset = new Vector3(0f, 3.2f, -7.5f);
    public Vector3 cameraLookOffset = new Vector3(0f, 1.2f, 1.5f);
    public float roomWidth = 24f;
    public float roomLength = 30f;
    public float roomHeight = 7f;

    private Transform root;
    private Material concrete;
    private Material darkMetal;
    private Material warning;
    private Material emissiveBlue;
    private Material emissiveRed;
    private bool built;

    private void Start()
    {
        Build();
    }

    public void Build()
    {
        if (built) return;
        built = true;
        if (anchor == null)
        {
            GameManager manager = FindObjectOfType<GameManager>();
            if (manager != null) anchor = manager.lobbySpawnPoint;
        }
        Vector3 origin = anchor != null ? anchor.position : Vector3.zero;
        Scene waitingScene = SceneManager.GetSceneByName("PlaytimeWaitingRoom");
        if (!waitingScene.IsValid() || !waitingScene.isLoaded)
            waitingScene = SceneManager.CreateScene("PlaytimeWaitingRoom");

        root = new GameObject("PLAYTIME_NEW_WAITING_ROOM").transform;
        root.position = origin;
        SceneManager.MoveGameObjectToScene(root.gameObject, waitingScene);

        concrete = Mat("Painted factory concrete", new Color(0.12f, 0.14f, 0.15f));
        darkMetal = Mat("Dark industrial metal", new Color(0.035f, 0.045f, 0.052f));
        warning = Mat("Safety orange", new Color(0.82f, 0.22f, 0.035f));
        emissiveBlue = Mat("Blue power glow", new Color(0.04f, 0.35f, 0.9f), true);
        emissiveRed = Mat("Red warning glow", new Color(0.85f, 0.035f, 0.015f), true);

        Block("Main floor", new Vector3(0f, -0.2f, 0f), new Vector3(roomWidth, 0.4f, roomLength), darkMetal);
        Block("Ceiling", new Vector3(0f, roomHeight, 0f), new Vector3(roomWidth, 0.35f, roomLength), darkMetal);
        Block("Back wall", new Vector3(0f, roomHeight * 0.5f, roomLength * 0.5f), new Vector3(roomWidth, roomHeight, 0.5f), concrete);
        Block("Left wall", new Vector3(-roomWidth * 0.5f, roomHeight * 0.5f, 0f), new Vector3(0.5f, roomHeight, roomLength), concrete);
        Block("Right wall", new Vector3(roomWidth * 0.5f, roomHeight * 0.5f, 0f), new Vector3(0.5f, roomHeight, roomLength), concrete);
        Block("Front low barrier", new Vector3(0f, 1f, -roomLength * 0.5f), new Vector3(roomWidth, 2f, 0.5f), concrete);

        // Waiting platform and seating.
        Block("Player waiting platform", new Vector3(0f, 0.18f, -2.5f), new Vector3(12f, 0.35f, 6f), warning);
        for (int i = 0; i < 4; i++)
        {
            float x = -6f + i * 4f;
            Block("Waiting bench " + i, new Vector3(x, 0.8f, 5f), new Vector3(2.8f, 0.25f, 0.8f), darkMetal);
            Block("Bench support " + i, new Vector3(x, 0.4f, 5f), new Vector3(0.25f, 0.8f, 0.25f), warning);
        }

        // Electrical demonstration area: generator, fuse cabinet, battery sockets and conduits.
        Block("Power generator body", new Vector3(-7f, 1.4f, 8f), new Vector3(3.5f, 2.8f, 2f), darkMetal);
        Block("Generator top", new Vector3(-7f, 2.9f, 8f), new Vector3(3.8f, 0.2f, 2.2f), warning);
        Block("Fuse cabinet", new Vector3(7f, 2.1f, 8f), new Vector3(2.4f, 4.2f, 1.2f), darkMetal);
        for (int i = 0; i < 3; i++)
        {
            Block("Fuse indicator " + i, new Vector3(6.35f + i * 0.65f, 2.5f, 7.35f),
                new Vector3(0.35f, 0.35f, 0.12f), i == 1 ? emissiveRed : emissiveBlue);
            Block("Battery socket " + i, new Vector3(6.2f + i * 0.8f, 0.75f, 8.65f),
                new Vector3(0.55f, 0.35f, 0.55f), warning);
        }
        Block("Power conduit A", new Vector3(-2f, 2.6f, 8f), new Vector3(7f, 0.18f, 0.18f), warning);
        Block("Power conduit B", new Vector3(1.4f, 2.6f, 8f), new Vector3(0.18f, 0.18f, 7f), warning);

        // Decorative doors, wall braces, ceiling strips.
        for (int i = 0; i < 5; i++)
        {
            float z = -10f + i * 5f;
            Block("Wall brace L " + i, new Vector3(-11.6f, 3.4f, z), new Vector3(0.3f, 6.5f, 0.3f), warning);
            Block("Wall brace R " + i, new Vector3(11.6f, 3.4f, z), new Vector3(0.3f, 6.5f, 0.3f), warning);
            Block("Ceiling strip " + i, new Vector3(0f, roomHeight - 0.25f, z), new Vector3(5f, 0.12f, 0.35f), emissiveBlue);
            AddLight("Waiting light " + i, new Vector3(0f, roomHeight - 0.8f, z), new Color(0.48f, 0.7f, 1f), 7f, 1.6f);
        }

        AddLight("Generator red alarm", new Vector3(-7f, 3.2f, 8f), new Color(1f, 0.08f, 0.02f), 6f, 2.2f);
        AddLight("Fuse blue power", new Vector3(7f, 3.5f, 8f), new Color(0.08f, 0.35f, 1f), 6f, 2.4f);

        if (lobbyCamera == null)
        {
            GameObject cameraObject = new GameObject("PoppyLobbyFixedCamera", typeof(Camera), typeof(AudioListener));
            lobbyCamera = cameraObject.GetComponent<Camera>();
            SceneManager.MoveGameObjectToScene(cameraObject, waitingScene);
        }
        lobbyCamera.transform.position = origin + cameraOffset;
        lobbyCamera.transform.LookAt(origin + cameraLookOffset);
        lobbyCamera.fieldOfView = 58f;
        lobbyCamera.enabled = false;
        AudioListener listener = lobbyCamera.GetComponent<AudioListener>();
        if (listener != null) listener.enabled = false;
        StartCoroutine(PowerFlicker());
    }

    private IEnumerator PowerFlicker()
    {
        Light[] lights = root.GetComponentsInChildren<Light>();
        while (true)
        {
            yield return new WaitForSecondsRealtime(Random.Range(3f, 8f));
            if (lights.Length == 0) continue;
            Light light = lights[Random.Range(0, lights.Length)];
            if (light == null) continue;
            light.enabled = false;
            yield return new WaitForSecondsRealtime(0.06f);
            if (light != null) light.enabled = true;
        }
    }

    private void Block(string name, Vector3 localPosition, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(root, false);
        obj.transform.localPosition = localPosition;
        obj.transform.localScale = scale;
        Renderer renderer = obj.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
    }

    private void AddLight(string name, Vector3 localPosition, Color color, float range, float intensity)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(root, false);
        obj.transform.localPosition = localPosition;
        Light light = obj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.range = range;
        light.intensity = intensity;
    }

    private Material Mat(string name, Color color, bool glow = false)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader);
        material.name = name;
        material.color = color;
        if (glow)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 1.7f);
        }
        return material;
    }
}
