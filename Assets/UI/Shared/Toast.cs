using UnityEngine;
using UnityEngine.UIElements;

public enum ToastKind { Info, Success, Danger }

// The one alert popup used everywhere (validation errors, "progress reset", ...). Bottom-left of the
// screen, auto-hides. Builds a design-system `ds-toast` the first time it's shown in a given root, so no
// UXML is needed. Styling lives in Assets/UI/Shared/Resources/UiUtilities.uss (.toast-overlay).
//
//   Toast.Show(root, "Progress reset", ToastKind.Success);
//   Toast.Show(root, error.message, ToastKind.Danger);
//
// `root` is the screen's styled "root" element (uiDocument.rootVisualElement.Q("root")). Uses the UI
// Toolkit scheduler (real time), so it still hides while Time.timeScale is 0 (e.g. with the Settings modal open).
public static class Toast
{
    const string PanelName = "toast-panel";

    public static void Show(VisualElement root, string message, ToastKind kind = ToastKind.Info, float seconds = 3f)
    {
        if (root == null) return;

        var utilities = Resources.Load<StyleSheet>("UiUtilities");
        if (utilities != null && !root.styleSheets.Contains(utilities)) root.styleSheets.Add(utilities);

        VisualElement panel = root.Q(PanelName) ?? Create(root);
        panel.EnableInClassList("ds-toast--danger", kind == ToastKind.Danger);
        panel.EnableInClassList("ds-toast--success", kind == ToastKind.Success);
        panel.EnableInClassList("ds-toast--info", kind == ToastKind.Info);

        VisualElement icon = panel.Q("toast-icon");
        icon.EnableInClassList("ds-icon--warning", kind == ToastKind.Danger);
        icon.EnableInClassList("ds-icon--check", kind == ToastKind.Success);
        icon.EnableInClassList("ds-icon--info", kind == ToastKind.Info);

        AudioManager.Play(kind switch { ToastKind.Danger => "toast_error", ToastKind.Success => "toast_success", _ => "toast_info" });

        panel.Q<Label>("toast-message").text = message;
        panel.style.display = DisplayStyle.Flex;
        panel.BringToFront(); // above any modal scrim added after the toast was created

        (panel.userData as IVisualElementScheduledItem)?.Pause();
        panel.userData = panel.schedule.Execute(() => panel.style.display = DisplayStyle.None).StartingIn((long)(seconds * 1000f));
    }

    public static void Hide(VisualElement root)
    {
        VisualElement panel = root?.Q(PanelName);
        if (panel == null) return;
        (panel.userData as IVisualElementScheduledItem)?.Pause();
        panel.style.display = DisplayStyle.None;
    }

    static VisualElement Create(VisualElement root)
    {
        var panel = new VisualElement { name = PanelName, pickingMode = PickingMode.Ignore };
        panel.AddToClassList("ds-toast");
        panel.AddToClassList("toast-overlay");
        panel.style.display = DisplayStyle.None;

        var icon = new VisualElement { name = "toast-icon", pickingMode = PickingMode.Ignore };
        icon.AddToClassList("ds-toast__icon");
        icon.AddToClassList("ds-icon");
        panel.Add(icon);

        var label = new Label { name = "toast-message", pickingMode = PickingMode.Ignore };
        label.AddToClassList("ds-toast__message");
        panel.Add(label);

        root.Add(panel);
        return panel;
    }
}
