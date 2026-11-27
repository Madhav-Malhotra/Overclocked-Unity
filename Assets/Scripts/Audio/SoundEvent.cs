using UnityEngine;

public enum SoundBus { Sfx, Ui, Music }

// One named sound: a set of interchangeable clips plus how to play them. The asset's file name is its key
// (Assets/Resources/Audio/SoundEvents/brick_pickup.asset -> AudioManager.Play("brick_pickup")).
// To change what a sound sounds like, edit the clip list in the Inspector - no code changes needed.
[CreateAssetMenu(menuName = "Overclocked/Sound Event", fileName = "new_sound")]
public class SoundEvent : ScriptableObject
{
    [SerializeField] private AudioClip[] clips;
    [SerializeField] private SoundBus bus = SoundBus.Sfx;
    [SerializeField, Range(0f, 1f)] private float minVolume = 0.9f;
    [SerializeField, Range(0f, 1f)] private float maxVolume = 1f;
    [SerializeField, Range(0.5f, 2f)] private float minPitch = 0.95f;
    [SerializeField, Range(0.5f, 2f)] private float maxPitch = 1.05f;

    // Debounce: when ticked, this sound is silently dropped (not queued) if any sound on the same bus is
    // still playing. Used for ui_hover so it never layers over a click.
    [SerializeField] private bool dropIfBusy;

    private int lastIndex = -1;

    public SoundBus Bus => bus;
    public bool DropIfBusy => dropIfBusy;
    public bool HasClips => clips != null && clips.Length > 0;

    // Picks a random clip (never the same one twice in a row) with slight volume/pitch variation.
    public bool TryPick(out AudioClip clip, out float volume, out float pitch)
    {
        clip = null; volume = 1f; pitch = 1f;
        if (!HasClips) return false;

        int index = Random.Range(0, clips.Length);
        if (clips.Length > 1 && index == lastIndex) index = (index + 1) % clips.Length;
        lastIndex = index;

        clip = clips[index];
        volume = Random.Range(minVolume, Mathf.Max(minVolume, maxVolume));
        pitch = Random.Range(minPitch, Mathf.Max(minPitch, maxPitch));
        return clip != null;
    }
}
