using UnityEngine;
using UnityEngine.UIElements;

public class InteractionUIManager : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    [Header("Prompt Text")]
    [SerializeField] private string pickUpText = "E - Pick Up";
    [SerializeField] private string placeText = "E - Place";

    private Label promptLabel;

    void Awake()
    {
        promptLabel = uiDocument.rootVisualElement.Q<Label>("prompt-label");
        if (promptLabel == null)
            Debug.LogError("InteractionUIManager: prompt-label not found in UXML");
    }

    void Start()
    {
        HidePrompt();
    }

    public void ShowPrompt(string text)
    {
        if (promptLabel == null) return;
        promptLabel.text = text;
        promptLabel.style.opacity = 1f;
    }

    public void HidePrompt()
    {
        if (promptLabel == null) return;
        promptLabel.style.opacity = 0f;
    }

    public void UpdatePrompt(bool isHoldingDisk, bool canInteract)
    {
        if (!canInteract)
        {
            HidePrompt();
            return;
        }

        string text = isHoldingDisk ? placeText : pickUpText;
        ShowPrompt(text);
    }
}
