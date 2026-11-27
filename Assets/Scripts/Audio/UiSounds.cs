using UnityEngine;
using UnityEngine.UIElements;

// Click + hover sounds for every Button (and any element with the `.clickable` class) under a UI Toolkit root,
// with no per-button code. Call once per screen:
//
//   UiSounds.Attach(uiDocument.rootVisualElement);
//
// Already wired in HelpOverlay.Build (all game screens + main menu) and EndScreenUI.
// Sound keys: ui_click, ui_hover (see Assets/Resources/Audio/SoundEvents/).
public static class UiSounds
{
    const string AttachedClass = "ui-sounds-attached";
    const string ClickableClass = "clickable";
    const float HoverCooldown = 0.05f; // seconds; stops a burst when the pointer sweeps over several buttons
    static float lastHover;
    static VisualElement hovered;

    public static void Attach(VisualElement root)
    {
        if (root == null || root.ClassListContains(AttachedClass)) return;
        root.AddToClassList(AttachedClass);

        // Trickle-down so it still fires when a handler inside the button stops propagation.
        root.RegisterCallback<ClickEvent>(OnClick, TrickleDown.TrickleDown);
        root.RegisterCallback<PointerOverEvent>(OnPointerOver);
    }

    static void OnClick(ClickEvent e)
    {
        if (FindClickable(e.target as VisualElement) != null) AudioManager.Play("ui_click");
    }

    static void OnPointerOver(PointerOverEvent e)
    {
        var target = FindClickable(e.target as VisualElement);
        if (target == null) { hovered = null; return; } // pointer moved onto non-clickable area

        // Moving between children of the same button isn't a new hover.
        if (target == hovered) return;
        hovered = target;

        if (Time.unscaledTime - lastHover < HoverCooldown) return;
        lastHover = Time.unscaledTime;
        AudioManager.Play("ui_hover");
    }

    static VisualElement FindClickable(VisualElement ve)
    {
        for (; ve != null; ve = ve.parent)
        {
            if (!ve.enabledInHierarchy) return null;
            if (ve is Button || ve.ClassListContains(ClickableClass)) return ve;
        }
        return null;
    }
}
