using UnityEngine;

public class UVReveal : MonoBehaviour
{
    private Renderer rend;

    private float glow = 0f;
    private float targetGlow = 0f;

    public float fadeSpeed = 5f;

    void Start()
    {
        rend = GetComponent<Renderer>();
    }

    void Update()
    {
        glow = Mathf.Lerp(glow, targetGlow, Time.deltaTime * fadeSpeed);

        targetGlow = 0f;

        Color col = Color.white;
        col.a = glow;
        rend.material.color = col;

        rend.material.SetColor("_EmissionColor", Color.white * glow * 2f);
    }

    public void Reveal()
    {
        targetGlow = .7f;
    }
}