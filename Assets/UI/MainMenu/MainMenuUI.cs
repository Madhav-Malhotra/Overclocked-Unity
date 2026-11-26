using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    void Awake()
    {
        var root = uiDocument.rootVisualElement;
        // Start opens the level picker (levels lock until the previous one is completed).
        root.Q<Button>("start-btn")?.RegisterCallback<ClickEvent>(_ => HelpOverlay.Instance?.OpenLevels());
        // HelpOverlay lives on this same GameObject (added next to the UIDocument) and builds the modals.
        root.Q<Button>("tutorial-btn")?.RegisterCallback<ClickEvent>(_ => HelpOverlay.Instance?.OpenTutorial());
        root.Q<Button>("settings-btn")?.RegisterCallback<ClickEvent>(_ => HelpOverlay.Instance?.OpenSettings());
        root.Q<Button>("about-btn")?.RegisterCallback<ClickEvent>(_ => HelpOverlay.Instance?.OpenAbout());
    }
}
