using UnityEngine;
using UnityEngine.UIElements;

public class GameHUD : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private VisualElement root;
    private Label timerLabel;
    private Label progressLabel;

    void Awake()
    {
        root = uiDocument.rootVisualElement;
        timerLabel = root.Q<Label>("timer-label");
        progressLabel = root.Q<Label>("progress-label");
    }

    void Update()
    {
        if (LevelManager.Instance == null) return;

        float t = Mathf.Max(0f, LevelManager.Instance.TimeRemaining);
        int minutes = Mathf.FloorToInt(t / 60f);
        int seconds = Mathf.FloorToInt(t % 60f);
        if (timerLabel != null)
            timerLabel.text = $"{minutes:00}:{seconds:00}";

        if (progressLabel != null)
            progressLabel.text = $"{LevelManager.Instance.CompletedCount} / {LevelManager.Instance.TotalCount}";
    }

    public void SetVisible(bool visible)
    {
        if (root != null)
            root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
