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

    // Runtime geometry is intentionally disabled. Build the waiting room manually in Unity.
    // This compatibility component remains for existing serialized scene references.
    public void Build()
    {
        built = true;
        Debug.Log("PoppyLobbyRoomBuilder: runtime room generation is disabled. Build the scene in Unity.", this);
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

    private void DisplayHand(string handName, Vector3 position, Material material)
    {
        GameObject palm = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        palm.name = handName + " palm";
        palm.transform.SetParent(root, false);
        palm.transform.localPosition = position;
        palm.transform.localScale = new Vector3(0.8f, 0.9f, 0.42f);
        if (palm.GetComponent<Renderer>() != null && material != null) palm.GetComponent<Renderer>().sharedMaterial = material;

        for (int i = 0; i < 4; i++)
        {
            GameObject finger = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            finger.name = handName + " finger " + i;
            finger.transform.SetParent(root, false);
            finger.transform.localPosition = position + new Vector3(-0.27f + i * 0.18f, 0.52f, 0f);
            finger.transform.localScale = new Vector3(0.14f, 0.3f, 0.14f);
            if (finger.GetComponent<Renderer>() != null && material != null) finger.GetComponent<Renderer>().sharedMaterial = material;
        }

        GameObject thumb = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        thumb.name = handName + " thumb";
        thumb.transform.SetParent(root, false);
        thumb.transform.localPosition = position + new Vector3(-0.48f, -0.05f, 0f);
        thumb.transform.localRotation = Quaternion.Euler(0f, 0f, -38f);
        thumb.transform.localScale = new Vector3(0.16f, 0.28f, 0.16f);
        if (thumb.GetComponent<Renderer>() != null && material != null) thumb.GetComponent<Renderer>().sharedMaterial = material;

        GameObject cable = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cable.name = handName + " cable";
        cable.transform.SetParent(root, false);
        Vector3 cableStart = position + new Vector3(0f, -0.35f, 0f);
        Vector3 cableEnd = position + new Vector3(0f, -1.15f, 0f);
        cable.transform.localPosition = (cableStart + cableEnd) * 0.5f;
        cable.transform.localScale = new Vector3(0.055f, 0.42f, 0.055f);
        if (cable.GetComponent<Renderer>() != null && darkMetal != null) cable.GetComponent<Renderer>().sharedMaterial = darkMetal;
    }

    private void Block(string name, Vector3 localPosition, Vector3 scale, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(root, false);
        obj.transform.localPosition = localPosition;
        obj.transform.localScale = scale;
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null && material != null) renderer.sharedMaterial = material;
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
        if (shader == null) shader = Shader.Find("Hidden/InternalErrorShader");
        if (shader == null)
        {
            Debug.LogError("PoppyLobbyRoomBuilder: no compatible shader is available.");
            return null;
        }
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
