using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    void Awake()
    {
        var root = uiDocument.rootVisualElement;
        root.Q<Button>("start-btn")?.RegisterCallback<ClickEvent>(_ => SceneLoader.LoadGame(0));
        root.Q<Button>("tutorial-btn")?.RegisterCallback<ClickEvent>(_ => Debug.Log("Tutorial not yet implemented"));
        root.Q<Button>("settings-btn")?.RegisterCallback<ClickEvent>(_ => Debug.Log("Settings not yet implemented"));
        root.Q<Button>("about-btn")?.RegisterCallback<ClickEvent>(_ => Debug.Log("About not yet implemented"));
    }
}
