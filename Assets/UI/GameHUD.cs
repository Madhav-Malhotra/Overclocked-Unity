using UnityEngine;
using TMPro;

public class GameHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI progressText;

    void Update()
    {
        if (LevelManager.Instance == null) return;

        float t = Mathf.Max(0f, LevelManager.Instance.TimeRemaining);
        int minutes = Mathf.FloorToInt(t / 60f);
        int seconds = Mathf.FloorToInt(t % 60f);
        if (timerText != null)
            timerText.text = $"{minutes:00}:{seconds:00}";

        if (progressText != null)
            progressText.text = $"{LevelManager.Instance.CompletedCount} / {LevelManager.Instance.TotalCount}";
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}
