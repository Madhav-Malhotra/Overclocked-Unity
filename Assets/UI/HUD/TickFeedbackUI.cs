using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Shows tick-validation errors as a toast (see Assets/UI/Shared/Toast.cs, the shared alert popup).
public class TickFeedbackUI : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private float autohideDuration = 3f;

    private VisualElement root;

    void Awake()
    {
        var docRoot = uiDocument.rootVisualElement;
        root = docRoot.Q("root") ?? docRoot;
    }

    public void ShowErrors(List<ValidationError> errors)
    {
        if (errors == null || errors.Count == 0) return;
        Toast.Show(root, errors[0].message, ToastKind.Danger, autohideDuration);
    }

    public void Hide() => Toast.Hide(root);
}
