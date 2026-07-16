using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EndScreenUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject successPanel;
    [SerializeField] private GameObject failurePanel;

    [Header("Success Panel")]
    [SerializeField] private TextMeshProUGUI successHeaderText;
    [SerializeField] private TextMeshProUGUI successStatText;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button retryButtonSuccess;

    [Header("Failure Panel")]
    [SerializeField] private TextMeshProUGUI failureHeaderText;
    [SerializeField] private TextMeshProUGUI failureStatText;
    [SerializeField] private Button retryButtonFailure;

    void Awake()
    {
        retryButtonSuccess?.onClick.AddListener(OnRetry);
        retryButtonFailure?.onClick.AddListener(OnRetry);
        nextLevelButton?.onClick.AddListener(OnNextLevel);

        if (LevelTransferData.Success)
        {
            int minutes = Mathf.FloorToInt(LevelTransferData.TimeTaken / 60f);
            int seconds = Mathf.FloorToInt(LevelTransferData.TimeTaken % 60f);
            ShowSuccess($"Time: {minutes:00}:{seconds:00}");
        }
        else
        {
            ShowFailure($"Instructions completed: {LevelTransferData.CompletedCount}/{LevelTransferData.TotalCount}");
        }
    }

    private void ShowSuccess(string stat)
    {
        if (successHeaderText != null) successHeaderText.text = "Level Complete!";
        if (successStatText != null) successStatText.text = stat;
        successPanel?.SetActive(true);
        failurePanel?.SetActive(false);

        bool hasNextLevel = LevelManager.Instance != null
            && LevelTransferData.NextLevelIndex + 1 < LevelManager.Instance.TotalLevelCount;
        nextLevelButton?.gameObject.SetActive(hasNextLevel);
    }

    private void ShowFailure(string stat)
    {
        if (failureHeaderText != null) failureHeaderText.text = "Try Again!";
        if (failureStatText != null) failureStatText.text = stat;
        successPanel?.SetActive(false);
        failurePanel?.SetActive(true);
    }

    private void OnRetry()
    {
        SceneLoader.LoadGame(LevelTransferData.NextLevelIndex);
    }

    private void OnNextLevel()
    {
        SceneLoader.LoadGame(LevelTransferData.NextLevelIndex + 1);
    }
}
