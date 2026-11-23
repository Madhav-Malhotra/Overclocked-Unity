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

    private VisualElement scrim, tutorialModal, settingsModal, aboutModal;
    private VisualElement searchWrap;
    private TextField searchField;
    private ScrollView topicList, topicContent;
    private Button tipsToggle;
    private Slider volumeSlider;

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
        if (sheet != null) host.styleSheets.Add(sheet);
        tree.CloneTree(host);
        Root = host.Q("help-root");

        scrim = Root.Q("help-scrim");
        tutorialModal = Root.Q("tutorial-modal");
        settingsModal = Root.Q("settings-modal");
        aboutModal = Root.Q("about-modal");
        searchWrap = Root.Q("search-wrap");
        searchField = Root.Q<TextField>("search-field");
        topicList = Root.Q<ScrollView>("topic-list");
        topicContent = Root.Q<ScrollView>("topic-content");
        tipsToggle = Root.Q<Button>("tips-toggle");
        volumeSlider = Root.Q<Slider>("volume-slider");

        foreach (string name in new[] { "tutorial-close", "tutorial-close-2", "settings-close", "settings-close-2", "about-close" })
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

        tipsToggle.RegisterCallback<ClickEvent>(_ =>
        {
            GameSettings.ShowTips = !GameSettings.ShowTips;
            RefreshSettings();
        });
        RefreshSettings();
    }

    void RefreshSettings()
    {
        volumeSlider.SetValueWithoutNotify(GameSettings.MasterVolume);
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
        foreach (var m in new[] { tutorialModal, settingsModal, aboutModal })
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
