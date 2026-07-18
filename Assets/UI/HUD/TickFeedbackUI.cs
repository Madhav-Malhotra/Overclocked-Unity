using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class TickFeedbackUI : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private float autohideDuration = 3f;

    private VisualElement toastPanel;
    private Label toastMessage;
    private Coroutine autohideCoroutine;

    void Awake()
    {
        var root = uiDocument.rootVisualElement;
        toastPanel = root.Q<VisualElement>("toast-panel");
        toastMessage = root.Q<Label>("toast-message");
    }

    public void ShowErrors(List<ValidationError> errors)
    {
        if (errors == null || errors.Count == 0) return;

        if (autohideCoroutine != null)
            StopCoroutine(autohideCoroutine);

        var first = errors[0];

        if (toastMessage != null)
            toastMessage.text = first.message;

        if (toastPanel != null)
            toastPanel.style.display = DisplayStyle.Flex;

        autohideCoroutine = StartCoroutine(AutoHide());
    }

    public void Hide()
    {
        if (autohideCoroutine != null)
        {
            StopCoroutine(autohideCoroutine);
            autohideCoroutine = null;
        }

        if (toastPanel != null)
            toastPanel.style.display = DisplayStyle.None;
    }

    private IEnumerator AutoHide()
    {
        yield return new WaitForSeconds(autohideDuration);
        Hide();
    }
}
