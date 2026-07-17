using UnityEngine;
using UnityEngine.UIElements;

public class EndScreenUI : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private GameObject successBackground;
    [SerializeField] private GameObject failureBackground;

    private VisualElement successPanel;
    private VisualElement failurePanel;
    private Label successHeaderText;
    private Label successStatText;
    private Button nextLevelButton;
    private Button retryButtonSuccess;
    private Label failureHeaderText;
    private Label failureStatText;
    private Button retryButtonFailure;

    void Awake()
    {
        var root = uiDocument.rootVisualElement;

        successPanel = root.Q<VisualElement>("success-panel");
        failurePanel = root.Q<VisualElement>("failure-panel");
        successHeaderText = root.Q<Label>("success-header");
        successStatText = root.Q<Label>("success-stat");
        nextLevelButton = root.Q<Button>("next-level-btn");
        retryButtonSuccess = root.Q<Button>("retry-btn-success");
        failureHeaderText = root.Q<Label>("failure-header");
        failureStatText = root.Q<Label>("failure-stat");
        retryButtonFailure = root.Q<Button>("retry-btn-failure");

        retryButtonSuccess?.RegisterCallback<ClickEvent>(_ => OnRetry());
        retryButtonFailure?.RegisterCallback<ClickEvent>(_ => OnRetry());
        nextLevelButton?.RegisterCallback<ClickEvent>(_ => OnNextLevel());

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
        SetDisplayed(successPanel, true);
        SetDisplayed(failurePanel, false);
        successBackground?.SetActive(true);
        failureBackground?.SetActive(false);

        bool hasNextLevel = LevelManager.Instance != null
            && LevelTransferData.NextLevelIndex + 1 < LevelManager.Instance.TotalLevelCount;
        SetDisplayed(nextLevelButton, hasNextLevel);
    }

    private void ShowFailure(string stat)
    {
        if (failureHeaderText != null) failureHeaderText.text = "Try Again!";
        if (failureStatText != null) failureStatText.text = stat;
        SetDisplayed(successPanel, false);
        SetDisplayed(failurePanel, true);
        successBackground?.SetActive(false);
        failureBackground?.SetActive(true);
    }

    private static void SetDisplayed(VisualElement element, bool visible)
    {
        if (element == null) return;
        element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
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
