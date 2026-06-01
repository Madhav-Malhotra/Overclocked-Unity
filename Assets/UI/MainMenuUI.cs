using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button startButton;

    void Awake()
    {
        startButton?.onClick.AddListener(() => SceneLoader.LoadGame(0));
    }
}
