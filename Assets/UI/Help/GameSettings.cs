using UnityEngine;

// Tiny PlayerPrefs-backed settings store. Add new settings here, then expose them in
// HelpOverlay's settings modal.
public static class GameSettings
{
    const string VolumeKey = "settings.masterVolume";
    const string MusicKey = "settings.musicVolume";
    const string SfxKey = "settings.sfxVolume";
    const string TipsKey = "settings.showTips";

    // Master scales everything via AudioListener; Music and SFX are extra gains applied by AudioManager
    // (SFX covers gameplay sounds, UI sounds and the movement hum).
    public static float MasterVolume
    {
        get => PlayerPrefs.GetFloat(VolumeKey, 1f);
        set
        {
            PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value));
            ApplyAudio();
        }
    }

    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat(MusicKey, 1f);
        set
        {
            PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value));
            ApplyAudio();
        }
    }

    public static float SfxVolume
    {
        get => PlayerPrefs.GetFloat(SfxKey, 1f);
        set => PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value));
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
        PlayerPrefs.DeleteKey(MusicKey);
        PlayerPrefs.DeleteKey(SfxKey);
        PlayerPrefs.DeleteKey(TipsKey);
        ApplyAudio();
    }

    public static void ApplyAudio()
    {
        AudioListener.volume = MasterVolume;
        AudioManager.RefreshMusicVolume();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void ApplyOnStartup() => ApplyAudio();
}
