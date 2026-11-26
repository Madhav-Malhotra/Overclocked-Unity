using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// Runs a level's optional step-by-step tip tour (LevelData.tips) and wires the HUD's help buttons.
// Sits on the same GameObject as the HUD UIDocument + HelpOverlay; no Inspector wiring needed.
//
// Each tip shows an "n / N" card. If the tip has a `target` (a TipTarget id) a pulsing ring is drawn
// around that object; `placement` puts the card beside it, otherwise the card is fixed top-right.
public class LevelTipsController : MonoBehaviour
{
    const float CardWidth = 560f, CardMargin = 24f, RingPad = 12f; // CardWidth must match .help-tip-card in HelpOverlay.uss
    const float FixedRight = 36f, FixedTop = 120f; // below the HUD help buttons

    [SerializeField] private UIDocument uiDocument;

    private LevelTip[] tips = Array.Empty<LevelTip>();
    private int index = -1;       // tip on screen, -1 = tour closed
    private int resumeIndex;      // where the Tips button restarts
    private string levelId;       // save key for the "tips seen" flag

    private VisualElement layer, ring, card;
    private Label stepLabel, titleLabel, textLabel;
    private Button nextBtn, backBtn, tipsBtn;

    private Bounds targetBounds;
    private bool hasTarget;

    void Start()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        root.Q<Button>("help-btn")?.RegisterCallback<ClickEvent>(_ => HelpOverlay.Instance?.OpenTutorial());
        tipsBtn = root.Q<Button>("tips-btn");
        tipsBtn?.RegisterCallback<ClickEvent>(_ => StartTour());

        layer = root.Q("tip-layer");
        if (layer == null)
        {
            Debug.LogWarning("LevelTipsController: tip layer not found - is HelpOverlay on the same GameObject?");
            return;
        }
        ring = root.Q("tip-ring");
        card = root.Q("tip-card");
        stepLabel = root.Q<Label>("tip-step");
        titleLabel = root.Q<Label>("tip-title");
        textLabel = root.Q<Label>("tip-text");
        nextBtn = root.Q<Button>("tip-next");
        backBtn = root.Q<Button>("tip-back");

        nextBtn.RegisterCallback<ClickEvent>(_ => ShowTip(index + 1));
        backBtn.RegisterCallback<ClickEvent>(_ => ShowTip(index - 1));
        root.Q<Button>("tip-close").RegisterCallback<ClickEvent>(_ => EndTour());

        // Re-place when the window resizes or the card's height changes (text wraps).
        layer.RegisterCallback<GeometryChangedEvent>(_ => Layout());
        card.RegisterCallback<GeometryChangedEvent>(_ => Layout());
        ring.schedule.Execute(() => ring.ToggleInClassList("is-pulsing")).Every(600);

        tips = LevelManager.Instance?.CurrentLevelData?.tips ?? Array.Empty<LevelTip>();
        levelId = LevelManager.Instance?.GetLevelId(LevelManager.Instance.CurrentLevelIndex);
        UpdateTipsButton();
        // Auto-start only the first time a level is played; the Tips button replays it any time.
        if (tips.Length > 0 && GameSettings.ShowTips && !SaveManager.HasLevelFlag(levelId, SaveManager.TipsSeenFlag)) StartTour();
    }

    // B / N = Back / Next while a tip is showing (and the help window isn't open).
    void Update()
    {
        var kb = Keyboard.current;
        if (index < 0 || kb == null || HelpOverlay.IsOpen) return;
        if (kb.nKey.wasPressedThisFrame) ShowTip(index + 1);
        else if (kb.bKey.wasPressedThisFrame) ShowTip(index - 1); // ShowTip ignores index < 0
    }

    // The Tips button only shows when the level has tips and the tour isn't already on screen.
    void UpdateTipsButton()
    {
        if (tipsBtn != null) tipsBtn.style.display = tips.Length > 0 && index < 0 ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void StartTour()
    {
        if (tips.Length == 0 || layer == null) return;
        layer.style.display = DisplayStyle.Flex;
        ShowTip(resumeIndex); // reopening after an X resumes at the tip you left
    }

    // finished = the player clicked Done on the last tip, so the next replay starts from tip 1.
    public void EndTour(bool finished = false)
    {
        if (index >= 0) resumeIndex = finished ? 0 : index;
        // Closing with X or finishing both count as seen, so the tour doesn't auto-start again.
        if (index >= 0) SaveManager.SetLevelFlag(levelId, SaveManager.TipsSeenFlag);
        index = -1;
        layer.style.display = DisplayStyle.None;
        UpdateTipsButton();
    }

    void ShowTip(int i)
    {
        if (i >= tips.Length) { EndTour(finished: true); return; }
        if (i < 0) return;

        index = i;
        UpdateTipsButton();
        LevelTip tip = tips[i];
        stepLabel.text = $"{i + 1} / {tips.Length}";
        titleLabel.text = tip.title;
        titleLabel.style.display = string.IsNullOrEmpty(tip.title) ? DisplayStyle.None : DisplayStyle.Flex;
        textLabel.text = tip.text;
        backBtn.SetEnabled(i > 0); // always shown so the buttons don't jump between tips
        nextBtn.text = i == tips.Length - 1 ? "Done (N)" : "Next (N)";

        hasTarget = false;
        if (!string.IsNullOrEmpty(tip.target))
        {
            TipTarget target = TipTarget.Find(tip.target);
            if (target == null)
                Debug.LogWarning($"LevelTipsController: no TipTarget with id '{tip.target}' in this scene (tip {i + 1}).");
            else
            {
                targetBounds = target.GetWorldBounds();
                hasTarget = true;
            }
        }
        Layout();
    }

    // Projects the target's world bounds into panel space (static camera/targets, so once per tip).
    bool TryProjectTarget(out Rect rect)
    {
        rect = default;
        var cam = Camera.main;
        if (cam == null || layer.panel == null) return false;

        Vector3 c = targetBounds.center, e = targetBounds.extents;
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = c + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
            Vector3 sp = cam.WorldToScreenPoint(corner);
            if (sp.z < 0f) return false; // behind the camera
            Vector2 p = RuntimePanelUtils.ScreenToPanel(layer.panel, new Vector2(sp.x, Screen.height - sp.y));
            minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
            minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
        }
        rect = Rect.MinMaxRect(minX - RingPad, minY - RingPad, maxX + RingPad, maxY + RingPad);
        return true;
    }

    void Layout()
    {
        if (index < 0) return;
        float panelW = layer.resolvedStyle.width, panelH = layer.resolvedStyle.height;
        float cardH = card.resolvedStyle.height;
        if (float.IsNaN(panelW) || panelW <= 0 || float.IsNaN(cardH)) return;

        float x = panelW - CardWidth - FixedRight, y = FixedTop; // default: fixed top right

        Rect r = default;
        bool projected = hasTarget && TryProjectTarget(out r);
        ring.style.display = projected ? DisplayStyle.Flex : DisplayStyle.None;
        if (projected)
        {
            ring.style.left = r.x; ring.style.top = r.y; ring.style.width = r.width; ring.style.height = r.height;
            switch ((tips[index].placement ?? "").ToLowerInvariant())
            {
                case "above": x = r.center.x - CardWidth / 2; y = r.y - cardH - CardMargin; break;
                case "below": x = r.center.x - CardWidth / 2; y = r.yMax + CardMargin; break;
                case "left":  x = r.x - CardWidth - CardMargin; y = r.center.y - cardH / 2; break;
                case "right": x = r.xMax + CardMargin; y = r.center.y - cardH / 2; break;
            }
        }

        card.style.left = Mathf.Clamp(x, CardMargin, Mathf.Max(CardMargin, panelW - CardWidth - CardMargin));
        card.style.top = Mathf.Clamp(y, CardMargin, Mathf.Max(CardMargin, panelH - cardH - CardMargin));
    }
}
