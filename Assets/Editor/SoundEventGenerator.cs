using System.IO;
using UnityEditor;
using UnityEngine;

// Tools > Audio > Generate Default Sound Events
// Creates one SoundEvent asset per row below in Assets/Resources/Audio/SoundEvents/. Existing assets are
// left alone, so Inspector tweaks survive re-running. To swap a sound later, just edit the asset's clips.
public static class SoundEventGenerator
{
    const string ClipDir = "Assets/Audio/kenney_interface-sounds/Audio/";
    const string OutDir = "Assets/Resources/Audio/SoundEvents";

    // key, bus, clip names (Kenney file names without .ogg)
    static readonly (string key, SoundBus bus, string[] clips)[] Defaults =
    {
        ("ui_click",       SoundBus.Ui,  new[] { "switch_005" }),
        ("ui_hover",       SoundBus.Ui,  new[] { "bong_001" }),
        ("ui_open",        SoundBus.Ui,  new[] { "maximize_003" }),
        ("ui_close",       SoundBus.Ui,  new[] { "minimize_003" }),
        ("brick_pickup",   SoundBus.Sfx, new[] { "drop_001" }),
        ("brick_place",    SoundBus.Sfx, new[] { "drop_001" }),
        ("tick_ok",        SoundBus.Sfx, new[] { "toggle_001" }),
        ("toast_success",  SoundBus.Ui,  new[] { "confirmation_002" }),
        ("toast_error",    SoundBus.Ui,  new[] { "error_002" }),
        ("toast_info",     SoundBus.Ui,  new[] { "glass_001" }),
        ("level_complete", SoundBus.Sfx, new[] { "confirmation_004" }),
        ("level_fail",     SoundBus.Sfx, new[] { "error_003" }),
        // A leading "@" means relative to Assets/Audio/ instead of the Kenney folder.
        ("music_main",     SoundBus.Music, new[] { "@AfternoonPings" }),
    };

    // Fixed base volume (min = max) for keys that shouldn't use the default 0.9-1.0 range.
    static readonly System.Collections.Generic.Dictionary<string, float> VolumeOverrides = new() { { "music_main", 0.3f } };

    // Keys that get the "Drop If Busy" debounce flag (dropped while anything on their bus is playing).
    static readonly System.Collections.Generic.HashSet<string> DropIfBusyKeys = new() { "ui_hover" };

    // Keys that always play at exactly pitch 1 (no random +-5% variation).
    static readonly System.Collections.Generic.HashSet<string> FixedPitchKeys = new() { "tick_ok" };

    [MenuItem("Tools/Audio/Generate Default Sound Events")]
    public static void Generate()
    {
        Directory.CreateDirectory(OutDir);
        AssetDatabase.Refresh();

        int created = 0, skipped = 0;
        foreach (var (key, bus, clipNames) in Defaults)
        {
            string path = $"{OutDir}/{key}.asset";
            if (AssetDatabase.LoadAssetAtPath<SoundEvent>(path) != null) { skipped++; continue; }

            var clips = new System.Collections.Generic.List<AudioClip>();
            foreach (var name in clipNames)
            {
                string clipPath = name.StartsWith("@") ? $"Assets/Audio/{name.Substring(1)}.ogg" : $"{ClipDir}{name}.ogg";
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                if (clip == null) Debug.LogWarning($"SoundEventGenerator: missing clip {name}.ogg for '{key}'.");
                else clips.Add(clip);
            }

            var asset = ScriptableObject.CreateInstance<SoundEvent>();
            var so = new SerializedObject(asset);
            so.FindProperty("bus").enumValueIndex = (int)bus;
            so.FindProperty("dropIfBusy").boolValue = DropIfBusyKeys.Contains(key);
            if (FixedPitchKeys.Contains(key) || bus == SoundBus.Music)
            {
                so.FindProperty("minPitch").floatValue = 1f;
                so.FindProperty("maxPitch").floatValue = 1f;
            }
            if (VolumeOverrides.TryGetValue(key, out float vol))
            {
                so.FindProperty("minVolume").floatValue = vol;
                so.FindProperty("maxVolume").floatValue = vol;
            }
            var arr = so.FindProperty("clips");
            arr.arraySize = clips.Count;
            for (int i = 0; i < clips.Count; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(asset, path);
            created++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"SoundEventGenerator: created {created}, skipped {skipped} existing in {OutDir}.");
    }
}
