using UnityEngine;

/// <summary>
/// Plays a looping hum while the player is moving, fading in/out smoothly.
/// Requires PlayerController on the same GameObject.
/// </summary>
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(PlayerController))]
public class PlayerMovementAudio : MonoBehaviour
{
    [SerializeField] private AudioClip movementClip;
    [SerializeField] private float targetVolume = 0.15f;
    [SerializeField] private float fadeSpeed = 2f;

    private AudioSource _audioSource;
    private PlayerController _controller;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _controller = GetComponent<PlayerController>();

        _audioSource.clip = movementClip;
        _audioSource.loop = true;
        _audioSource.playOnAwake = false;
        _audioSource.volume = 0f;
        _audioSource.spatialBlend = 0f; // 2D — heard equally everywhere
        _audioSource.Play();
    }

    private void Update()
    {
        bool isMoving = _controller.IsMoving;
        float desiredVolume = isMoving ? targetVolume : 0f;
        _audioSource.volume = Mathf.MoveTowards(_audioSource.volume, desiredVolume, fadeSpeed * Time.deltaTime);
    }
}
