using System;
using UnityEngine;

/// <summary>Local accessibility, audio and input preferences persisted between sessions.</summary>
public static class PoppySettings
{
    private const string SensitivityKey = "Poppy.Settings.MouseSensitivity";
    private const string DeadzoneKey = "Poppy.Settings.MouseDeadzone";
    private const string RotationSmoothKey = "Poppy.Settings.RotationSmooth";
    private const string VolumeKey = "Poppy.Settings.MasterVolume";
    private const string MusicVolumeKey = "Poppy.Settings.MusicVolume";
    private const string EffectsVolumeKey = "Poppy.Settings.EffectsVolume";
    private const string HighContrastKey = "Poppy.Settings.HighContrast";
    private const string LargeTextKey = "Poppy.Settings.LargeText";

    public static event Action PreferencesChanged;

    public static float MouseSensitivity => PlayerPrefs.GetFloat(SensitivityKey, 2f);
    public static float MouseDeadzone => PlayerPrefs.GetFloat(DeadzoneKey, 0.02f);
    public static float RotationSmoothTime => PlayerPrefs.GetFloat(RotationSmoothKey, 0.045f);
    public static float MasterVolume => PlayerPrefs.GetFloat(VolumeKey, 0.8f);
    public static float MusicVolume => PlayerPrefs.GetFloat(MusicVolumeKey, 0.8f);
    public static float EffectsVolume => PlayerPrefs.GetFloat(EffectsVolumeKey, 1f);
    public static bool HighContrast => PlayerPrefs.GetInt(HighContrastKey, 0) != 0;
    public static bool LargeText => PlayerPrefs.GetInt(LargeTextKey, 0) != 0;

    public static void SetMouseSensitivity(float value) { SetFloat(SensitivityKey, Mathf.Clamp(value, 0.2f, 5f)); }
    public static void SetMouseDeadzone(float value) { SetFloat(DeadzoneKey, Mathf.Clamp(value, 0f, 0.2f)); }
    public static void SetRotationSmoothTime(float value) { SetFloat(RotationSmoothKey, Mathf.Clamp(value, 0f, 0.25f)); }
    public static void SetMasterVolume(float value)
    {
        PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value));
        PlayerPrefs.Save();
        Apply();
    }
    public static void SetMusicVolume(float value) { SetFloat(MusicVolumeKey, Mathf.Clamp01(value)); }
    public static void SetEffectsVolume(float value) { SetFloat(EffectsVolumeKey, Mathf.Clamp01(value)); }
    public static void SetHighContrast(bool enabled) { PlayerPrefs.SetInt(HighContrastKey, enabled ? 1 : 0); SaveAndNotify(); }
    public static void SetLargeText(bool enabled) { PlayerPrefs.SetInt(LargeTextKey, enabled ? 1 : 0); SaveAndNotify(); }

    public static void Apply()
    {
        AudioListener.volume = MasterVolume;
        ApplyMouseSensitivity();
        PreferencesChanged?.Invoke();
    }

    private static void SetFloat(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
        ApplyMouseSensitivity();
        PreferencesChanged?.Invoke();
    }

    private static void SaveAndNotify()
    {
        PlayerPrefs.Save();
        PreferencesChanged?.Invoke();
    }

    private static void ApplyMouseSensitivity()
    {
        DbUnityMovment[] players = UnityEngine.Object.FindObjectsOfType<DbUnityMovment>();
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null && players[i].IsOwner)
                players[i].mouseSensitivity = MouseSensitivity;
        }
    }
}
