using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TableProcessingTimer : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image fillBar;
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Settings")]
    [SerializeField] private Vector3 offsetAboveTable = new Vector3(0, 3.5f, 0);
    [SerializeField] private Color processingColor = new Color(1f, 0.5f, 0f); // Orange

    private float duration;
    private float remainingTime;
    private bool isRunning = false;
    private Camera mainCamera;

    public void Initialize(float timerDuration, Transform tableTransform)
    {
        duration = timerDuration;
        remainingTime = timerDuration;
        isRunning = true;

        // Get main camera reference
        mainCamera = Camera.main;

        // Position above table
        transform.position = tableTransform.position + offsetAboveTable;

        // Set initial color
        if (fillBar != null)
            fillBar.color = processingColor;

        UpdateUI();
    }

    void Update()
    {
        if (!isRunning) return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        // Billboard effect - always face the camera
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                           mainCamera.transform.rotation * Vector3.up);
        }

        // Use deltaTime (game time, not unscaled)
        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            isRunning = false;
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        float progress = duration > 0f ? remainingTime / duration : 0f;

        if (fillBar != null)
            fillBar.fillAmount = progress;

        if (timerText != null)
            timerText.text = $"{remainingTime:F1}s";
    }

    public float GetRemainingTime()
    {
        return remainingTime;
    }

    public bool IsComplete()
    {
        return remainingTime <= 0f;
    }
}
