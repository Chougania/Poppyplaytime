using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class flickerLight : MonoBehaviour
{
    public Light flickeringLight;
    public float minIntensity = 0.5f;
    public float maxIntensity = 1.5f;
    public float speed = 10f;

    void Update()
    {
        float noise = Mathf.PerlinNoise(Time.time * speed, 0f);
        flickeringLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
    }
}
