using UnityEngine;

// Tiny PlayerPrefs-backed settings store. Add new settings here, then expose them in
// HelpOverlay's settings modal.
public static class GameSettings
{
    const string VolumeKey = "settings.masterVolume";
    const string TipsKey = "settings.showTips";

    public static float MasterVolume
    {
        get => PlayerPrefs.GetFloat(VolumeKey, 1f);
        set
        {
            PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value));
            ApplyAudio();
        }
    }

    // When true, a level's tip tour starts automatically on level load.
    public static bool ShowTips
    {
        get => PlayerPrefs.GetInt(TipsKey, 1) == 1;
        set => PlayerPrefs.SetInt(TipsKey, value ? 1 : 0);
    }

    public static void ResetToDefaults()
    {
        PlayerPrefs.DeleteKey(VolumeKey);
        PlayerPrefs.DeleteKey(TipsKey);
        ApplyAudio();
    }

    public static void ApplyAudio()
    {
        AudioListener.volume = MasterVolume;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void ApplyOnStartup() => ApplyAudio();
}
