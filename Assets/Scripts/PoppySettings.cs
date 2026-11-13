using UnityEngine;

/// <summary>Local audio and mouse settings shared by the pause menu and player controller.</summary>
public static class PoppySettings
{
    private const string SensitivityKey = "Poppy.Settings.MouseSensitivity";
    private const string VolumeKey = "Poppy.Settings.MasterVolume";

    public static float MouseSensitivity
    {
        get { return PlayerPrefs.GetFloat(SensitivityKey, 2f); }
    }

    public static float MasterVolume
    {
        get { return PlayerPrefs.GetFloat(VolumeKey, 0.8f); }
    }

    public static void SetMouseSensitivity(float value)
    {
        PlayerPrefs.SetFloat(SensitivityKey, Mathf.Clamp(value, 0.2f, 5f));
        PlayerPrefs.Save();
        ApplyMouseSensitivity();
    }

    public static void SetMasterVolume(float value)
    {
        PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value));
        PlayerPrefs.Save();
        AudioListener.volume = MasterVolume;
    }

    public static void Apply()
    {
        AudioListener.volume = MasterVolume;
        ApplyMouseSensitivity();
    }

    private static void ApplyMouseSensitivity()
    {
        DbUnityMovment[] players = Object.FindObjectsOfType<DbUnityMovment>();
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null && players[i].IsOwner)
                players[i].mouseSensitivity = MouseSensitivity;
        }
    }
}
