using UnityEngine;

public class NoiseEmitter : MonoBehaviour
{
    public static void EmitNoise(
        Vector3 position,
        float loudness,
        Transform source = null)
    {
        HearingSensor[] sensors = FindObjectsOfType<HearingSensor>();

        foreach (HearingSensor sensor in sensors)
        {
            sensor.HearNoise(position, loudness, source);
        }
    }
}