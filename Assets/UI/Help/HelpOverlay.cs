using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// Tutorial / Settings / About modals, injected into whatever UIDocument this component sits next to.
// Add it to the GameObject that owns a screen's UIDocument; it loads its own UXML/USS from
// Assets/UI/Help/Resources/, so no Inspector wiring is needed.
//
// API (static, safe to call from anywhere once a scene's overlay exists):
//   HelpOverlay.Instance.OpenTutorial("controls")   // topic id = markdown file name; null = first topic
//   HelpOverlay.Instance.OpenSettings() / OpenAbout() / CloseAll()
//   HelpOverlay.IsOpen                              // gameplay scripts should ignore input while true
//
// While open, Time.timeScale is 0 (pauses the level timer) and the player's PlayerInput is disabled.
[DisallowMultipleComponent]
public class HelpOverlay : MonoBehaviour
{
    public static HelpOverlay Instance { get; private set; }
    public static bool IsOpen => Instance != null && Instance.isOpen;

    [SerializeField] private UIDocument uiDocument;

    public VisualElement Root { get; private set; }

    private VisualElement scrim, tutorialModal, settingsModal, aboutModal, levelsModal;
    private ScrollView levelsList;
    private VisualElement searchWrap;
    private TextField searchField;
    private ScrollView topicList, topicContent;
    private Button tipsToggle;
    private Slider volumeSlider, musicSlider, sfxSlider;
    private VisualElement toastRoot;   // where Toast.Show puts alerts (the screen's styled "root")
    private Button resetProgressBtn;
    private bool resetArmed;

    private readonly Dictionary<string, VisualElement> navItems = new();
    private string currentTopicId;

    private bool isOpen;
    private float prevTimeScale = 1f;
    private PlayerInput pausedPlayerInput;

    void Awake()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        Instance = this;
        Build();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (isOpen) ResumeGame();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (isOpen)
        {
            if (kb.escapeKey.wasPressedThisFrame) CloseAll();
        }
        else if (kb.f1Key.wasPressedThisFrame)
        {
            OpenTutorial();
        }
        else if (kb.escapeKey.wasPressedThisFrame && !SceneLoader.InMainMenu && LevelManager.Instance != null && LevelManager.Instance.LevelActive)
        {
            LevelManager.Instance.AbandonLevel(); // Esc during a level returns to the main menu
        }
    }

    // ---------------------------------------------------------------- build

    void Build()
    {
        var docRoot = uiDocument.rootVisualElement;
        var tree = Resources.Load<VisualTreeAsset>("HelpOverlay");
        var sheet = Resources.Load<StyleSheet>("HelpOverlay");
        if (tree == null)
        {
            Debug.LogError("HelpOverlay: Resources/HelpOverlay.uxml not found (expected in Assets/UI/Help/Resources/).");
            return;
        }

        // Host inside the screen's styled "root" element when there is one, so design-system fonts apply.
        VisualElement host = docRoot.Q("root") ?? docRoot;
        toastRoot = host;
        UiSounds.Attach(docRoot); // click/hover sounds for every button on this screen
        // Shared utilities (.clickable, .on-hover, .accent-*) first so this overlay's own sheet can override them.
        var utilities = Resources.Load<StyleSheet>("UiUtilities");
        if (utilities != null) host.styleSheets.Add(utilities);
        if (sheet != null) host.styleSheets.Add(sheet);
        tree.CloneTree(host);
        Root = host.Q("help-root");

        scrim = Root.Q("help-scrim");
        tutorialModal = Root.Q("tutorial-modal");
        settingsModal = Root.Q("settings-modal");
        aboutModal = Root.Q("about-modal");
        levelsModal = Root.Q("levels-modal");
        levelsList = Root.Q<ScrollView>("levels-list");
        searchWrap = Root.Q("search-wrap");
        searchField = Root.Q<TextField>("search-field");
        topicList = Root.Q<ScrollView>("topic-list");
        topicContent = Root.Q<ScrollView>("topic-content");
        tipsToggle = Root.Q<Button>("tips-toggle");
        volumeSlider = Root.Q<Slider>("volume-slider");
        musicSlider = Root.Q<Slider>("music-slider");
        sfxSlider = Root.Q<Slider>("sfx-slider");

        foreach (string name in new[] { "tutorial-close", "tutorial-close-2", "settings-close", "settings-close-2", "about-close", "levels-close", "levels-close-2" })
            Root.Q<Button>(name)?.RegisterCallback<ClickEvent>(_ => CloseAll());

        // Click on the dimmed background (not a modal) closes.
        scrim.RegisterCallback<PointerDownEvent>(e => { if (e.target == scrim) CloseAll(); });

        BuildTutorial();
        BuildSettings();
    }

    void BuildTutorial()
    {
        searchField.textEdition.placeholder = "Search the tutorial...";
        searchField.RegisterValueChangedCallback(e => RefreshNav(e.newValue, keepSelection: true));
        searchField.RegisterCallback<FocusInEvent>(_ => searchWrap.AddToClassList("is-focused"));
        searchField.RegisterCallback<FocusOutEvent>(_ => searchWrap.RemoveFromClassList("is-focused"));
    }

    void BuildSettings()
    {
        volumeSlider.SetValueWithoutNotify(GameSettings.MasterVolume);
        volumeSlider.RegisterValueChangedCallback(e => GameSettings.MasterVolume = e.newValue);
        musicSlider.RegisterValueChangedCallback(e => GameSettings.MusicVolume = e.newValue);
        sfxSlider.RegisterValueChangedCallback(e => GameSettings.SfxVolume = e.newValue);

        tipsToggle.RegisterCallback<ClickEvent>(_ =>
        {
            GameSettings.ShowTips = !GameSettings.ShowTips;
            RefreshSettings();
        });
        resetProgressBtn = Root.Q<Button>("reset-progress-btn");
        resetProgressBtn.RegisterCallback<ClickEvent>(_ => OnResetProgressClicked());
        Root.Q<Button>("open-save-btn").RegisterCallback<ClickEvent>(_ => Application.OpenURL("file://" + SaveManager.SaveFolder));
        RefreshSettings();
    }

    // Two-step: first click arms the button, a second click within 3s wipes saved progress.
    void OnResetProgressClicked()
    {
        if (!resetArmed)
        {
            resetArmed = true;
            resetProgressBtn.text = "Click again to confirm";
            resetProgressBtn.schedule.Execute(DisarmReset).StartingIn(3000);
            return;
        }
        SaveManager.ResetAll();
        DisarmReset();
        Toast.Show(toastRoot, "Progress reset", ToastKind.Success);
    }

    void DisarmReset()
    {
        resetArmed = false;
        if (resetProgressBtn != null) resetProgressBtn.text = "Reset progress";
    }

    void RefreshSettings()
    {
        resetArmed = false;
        if (resetProgressBtn != null) resetProgressBtn.text = "Reset progress";
        volumeSlider.SetValueWithoutNotify(GameSettings.MasterVolume);
        musicSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
        sfxSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
        bool on = GameSettings.ShowTips;
        tipsToggle.text = on ? "On" : "Off";
        tipsToggle.EnableInClassList("ds-btn--primary", on);
        tipsToggle.EnableInClassList("ds-btn--secondary", !on);
    }

    // ---------------------------------------------------------------- public API

    public void OpenTutorial(string topicId = null)
    {
        if (Root == null) return;
        PauseGame();
        ShowModal(tutorialModal);
        searchField.SetValueWithoutNotify("");
        RefreshNav("", keepSelection: false);

        TutorialTopic topic = topicId != null ? TutorialLibrary.Find(topicId) : null;
        if (topic == null && topicId != null)
            Debug.LogWarning($"HelpOverlay: no tutorial topic with id '{topicId}'.");
        if (topic == null && TutorialLibrary.Topics.Count > 0) topic = TutorialLibrary.Topics[0];
        SelectTopic(topic?.id);
    }

    public void OpenSettings()
    {
        if (Root == null) return;
        PauseGame();
        RefreshSettings();
        ShowModal(settingsModal);
    }

    public void OpenAbout()
    {
        if (Root == null) return;
        PauseGame();
        var content = Root.Q<ScrollView>("about-content");
        content.Clear();
        var asset = Resources.Load<TextAsset>(TutorialLibrary.AboutPath);
        string body = asset != null ? TutorialLibrary.Parse("about", asset.text).body : "Add Assets/Resources/HelpContent/about.md";
        content.Add(MarkdownView.Build(body, null));
        ShowModal(aboutModal);
    }

    // Level picker (main menu Start). Level N is locked until level N-1 is completed.
    // Editor only: hold Shift while clicking a locked level to open it anyway (for testing).
    public void OpenLevels()
    {
        if (Root == null) return;
        PauseGame();
        levelsList.Clear();

        LevelManager lm = LevelManager.Instance;
        if (lm == null || lm.TotalLevelCount == 0)
        {
            var none = new Label("No levels found.");
            none.AddToClassList("help-empty");
            levelsList.Add(none);
        }
        else
        {
            for (int i = 0; i < lm.TotalLevelCount; i++) levelsList.Add(MakeLevelRow(lm, i));
        }
        ShowModal(levelsModal);
    }

    VisualElement MakeLevelRow(LevelManager lm, int index)
    {
        LevelData data = lm.GetLevelData(index);
        bool unlocked = index == 0 || SaveManager.IsLevelCompleted(lm.GetLevelId(index - 1));

        var row = new VisualElement();
        row.AddToClassList("help-level");
        row.AddToClassList("bordered");
        row.AddToClassList("mb-md");
        row.EnableInClassList("help-level--locked", !unlocked);
        // Locked cards get no hover or pointer cursor: they aren't clickable.
        row.EnableInClassList("clickable", unlocked);
        row.EnableInClassList("on-hover", unlocked);

        var num = new VisualElement();
        num.AddToClassList("help-level__num");
        var numLabel = new Label((index + 1).ToString());
        numLabel.AddToClassList("ds-h3");
        numLabel.AddToClassList("accent-primary");
        num.Add(numLabel);
        row.Add(num);

        var text = new VisualElement();
        text.AddToClassList("help-level__text");
        var title = new Label(string.IsNullOrEmpty(data?.levelName) ? lm.GetLevelId(index) : data.levelName);
        title.AddToClassList("ds-h3");
        text.Add(title);
        if (!string.IsNullOrEmpty(data?.blurb))
        {
            var blurb = new Label(data.blurb);
            blurb.AddToClassList("ds-body-1");
            blurb.AddToClassList("help-level__blurb");
            text.Add(blurb);
        }
        row.Add(text);

        if (!unlocked)
        {
            var lockIcon = new VisualElement();
            lockIcon.AddToClassList("ds-icon");
            lockIcon.AddToClassList("ds-icon--lock");
            lockIcon.AddToClassList("accent-primary");
            lockIcon.AddToClassList("help-level__lock");
            row.Add(lockIcon);
        }

        row.RegisterCallback<ClickEvent>(_ =>
        {
            bool bypass = false;
#if UNITY_EDITOR
            bypass = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
#endif
            if (!unlocked && !bypass) return;
            CloseAll(); // restores timeScale before the scene changes
            SceneLoader.LoadGame(index);
        });
        return row;
    }

    public void CloseAll()
    {
        if (Root == null) return;
        ShowModal(null);
        if (isOpen) ResumeGame();
    }

    // ---------------------------------------------------------------- tutorial nav

    void RefreshNav(string query, bool keepSelection)
    {
        topicList.Clear();
        navItems.Clear();

        List<TutorialTopic> results = TutorialLibrary.Search(query);
        if (results.Count == 0)
        {
            var none = new Label("No matching topics.");
            none.AddToClassList("help-empty");
            topicList.Add(none);
            return;
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            foreach (var (category, topics) in TutorialLibrary.Grouped(results))
            {
                var header = new Label(category.ToUpperInvariant());
                header.AddToClassList("help-nav__category");
                topicList.Add(header);
                foreach (TutorialTopic t in topics) topicList.Add(MakeNavItem(t));
            }
        }
        else
        {
            foreach (TutorialTopic t in results) topicList.Add(MakeNavItem(t));
        }

        if (keepSelection)
        {
            if (currentTopicId != null && navItems.ContainsKey(currentTopicId)) SetActiveNav(currentTopicId);
            else SelectTopic(results[0].id);
        }
    }

    VisualElement MakeNavItem(TutorialTopic topic)
    {
        var row = new VisualElement();
        row.AddToClassList("help-nav-item");
        row.AddToClassList("clickable");
        row.AddToClassList("on-hover");

        var icon = new VisualElement();
        icon.AddToClassList("ds-icon");
        icon.AddToClassList("ds-icon--" + topic.icon);
        icon.AddToClassList("help-nav-item__icon");
        row.Add(icon);

        var label = new Label(topic.title);
        label.AddToClassList("help-nav-item__label");
        row.Add(label);

        string id = topic.id;
        row.RegisterCallback<ClickEvent>(_ => SelectTopic(id));
        navItems[id] = row;
        return row;
    }

    void SetActiveNav(string id)
    {
        foreach (var kv in navItems) kv.Value.EnableInClassList("is-active", kv.Key == id);
    }

    void SelectTopic(string id)
    {
        topicContent.Clear();
        currentTopicId = id;
        TutorialTopic topic = id != null ? TutorialLibrary.Find(id) : null;
        if (topic == null)
        {
            var empty = new Label("No tutorial content found. Add .md files to Assets/Resources/HelpContent/Tutorial/.");
            empty.AddToClassList("help-empty");
            topicContent.Add(empty);
            return;
        }

        // Make sure a topic reached via a link is visible/selected in the nav (clears an active search).
        if (!navItems.ContainsKey(id))
        {
            searchField.SetValueWithoutNotify("");
            RefreshNav("", keepSelection: false);
        }
        SetActiveNav(id);

        var title = new Label(topic.title);
        title.AddToClassList("ds-h1");
        title.style.marginBottom = 14;
        topicContent.Add(title);
        topicContent.Add(MarkdownView.Build(topic.body, SelectTopic));
        topicContent.scrollOffset = Vector2.zero;
    }

    // ---------------------------------------------------------------- show/pause

    // Shows exactly one modal (null hides everything).
    void ShowModal(VisualElement modal)
    {
        scrim.style.display = modal != null ? DisplayStyle.Flex : DisplayStyle.None;
        foreach (var m in new[] { tutorialModal, settingsModal, aboutModal, levelsModal })
            m.style.display = m == modal ? DisplayStyle.Flex : DisplayStyle.None;
    }

    void PauseGame()
    {
        if (isOpen) return;
        isOpen = true;
        prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        pausedPlayerInput = FindFirstObjectByType<PlayerInput>();
        if (pausedPlayerInput != null && pausedPlayerInput.inputIsActive) pausedPlayerInput.DeactivateInput();
        else pausedPlayerInput = null;
    }

    void ResumeGame()
    {
        isOpen = false;
        Time.timeScale = prevTimeScale;

        // Don't re-enable input if the level ended while the overlay was open.
        bool levelOver = LevelManager.Instance != null && !LevelManager.Instance.LevelActive;
        if (pausedPlayerInput != null && !levelOver) pausedPlayerInput.ActivateInput();
        pausedPlayerInput = null;
    }
}
