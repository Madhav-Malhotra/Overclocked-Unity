using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

// Plays named SoundEvents. Bootstraps itself before the first scene loads (no scene wiring needed) and
// survives scene changes. Everything it loads lives under Assets/Resources/Audio/:
//   SoundEvents/<key>.asset   one SoundEvent per sound (see SoundEvent.cs)
//   MainMixer.mixer           optional; groups Master/SFX, Master/UI (and Master/Music later).
//                             Without it sounds still play, just without per-group volume control.
//
//   AudioManager.Play("brick_pickup");
//   AudioManager.GroupFor(SoundBus.Sfx)   // for scripts with their own AudioSource (e.g. PlayerMovementAudio)
//
// Music: one looping track on a dedicated source, started at boot and kept across scene loads
// (AudioManager.PlayMusic("key"); calling it again with the track already playing is a no-op).
// Volumes: SoundEvent volume x GameSettings.SfxVolume / MusicVolume (settings sliders), x AudioListener (master).
public class AudioManager : MonoBehaviour
{
    const int PoolSize = 8;
    const string MixerPath = "Audio/MainMixer";
    const string EventsPath = "Audio/SoundEvents";
    const string DefaultMusicKey = "music_main";

    static AudioManager instance;

    readonly Dictionary<string, SoundEvent> events = new();
    readonly HashSet<string> warned = new();
    AudioSource[] pool;
    int next;
    AudioMixer mixer;
    AudioSource musicSource;
    float musicBaseVolume = 1f; // the SoundEvent's own volume, before the Music slider is applied
    readonly float[] busyUntil = new float[3]; // per SoundBus: unscaled time when its last sound ends

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (instance != null) return;
        var go = new GameObject("AudioManager");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<AudioManager>();
    }

    void Awake()
    {
        // Set here, not only in Bootstrap: Awake runs inside AddComponent, before Bootstrap's assignment,
        // and PlayMusic below needs `instance`.
        instance = this;

        mixer = Resources.Load<AudioMixer>(MixerPath);

        foreach (var e in Resources.LoadAll<SoundEvent>(EventsPath))
            events[e.name] = e;

        pool = new AudioSource[PoolSize];
        for (int i = 0; i < PoolSize; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f; // 2D
            pool[i] = src;
        }

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;

        PlayMusic(DefaultMusicKey);
    }

    // Starts looping the music SoundEvent `key` (its first clip). No-op if that clip is already playing.
    public static void PlayMusic(string key)
    {
        if (instance == null) return;
        if (!instance.events.TryGetValue(key, out var e) || !e.TryPick(out var clip, out var volume, out _))
        {
            if (instance.warned.Add(key)) Debug.LogWarning($"AudioManager: no music SoundEvent '{key}' with a clip in Resources/{EventsPath}.");
            return;
        }

        var src = instance.musicSource;
        instance.musicBaseVolume = volume;
        src.outputAudioMixerGroup = GroupFor(SoundBus.Music);
        src.volume = volume * GameSettings.MusicVolume;
        if (src.clip == clip && src.isPlaying) return;

        src.clip = clip;
        src.Play();
    }

    // Re-applies the Music slider to the track that is already playing (called from GameSettings).
    public static void RefreshMusicVolume()
    {
        if (instance == null || instance.musicSource == null) return;
        instance.musicSource.volume = instance.musicBaseVolume * GameSettings.MusicVolume;
    }

    public static void Play(string key)
    {
        if (instance == null) return;
        if (!instance.events.TryGetValue(key, out var e))
        {
            if (instance.warned.Add(key)) Debug.LogWarning($"AudioManager: no SoundEvent named '{key}' in Resources/{EventsPath}.");
            return;
        }
        Play(e);
    }

    public static void Play(SoundEvent e)
    {
        if (instance == null || e == null) return;

        // Debounce: drop (don't queue) low-priority sounds while their bus is still busy. unscaledTime so it
        // keeps working while a modal has Time.timeScale at 0.
        float now = Time.unscaledTime;
        int bus = (int)e.Bus;
        if (e.DropIfBusy && now < instance.busyUntil[bus]) return;

        if (!e.TryPick(out var clip, out var volume, out var pitch)) return;
        instance.busyUntil[bus] = Mathf.Max(instance.busyUntil[bus], now + clip.length / Mathf.Max(pitch, 0.01f));

        // Round-robin through the pool; if every source is busy this cuts off the oldest one.
        var src = instance.pool[instance.next];
        instance.next = (instance.next + 1) % PoolSize;

        src.outputAudioMixerGroup = GroupFor(e.Bus);
        src.pitch = pitch;
        src.PlayOneShot(clip, volume * (e.Bus == SoundBus.Music ? GameSettings.MusicVolume : GameSettings.SfxVolume));
    }

    // Mixer group for a bus, or null when there is no mixer / no such group (audio then uses the default output).
    public static AudioMixerGroup GroupFor(SoundBus bus)
    {
        if (instance == null || instance.mixer == null) return null;
        string path = bus switch { SoundBus.Ui => "Master/UI", SoundBus.Music => "Master/Music", _ => "Master/SFX" };
        var groups = instance.mixer.FindMatchingGroups(path);
        return groups != null && groups.Length > 0 ? groups[0] : null;
    }
}
