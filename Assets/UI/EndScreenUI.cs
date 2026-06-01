using UnityEngine;
using UnityEngine.UI;

public class EndScreenUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject successPanel;
    [SerializeField] private GameObject failurePanel;

    [Header("Buttons")]
    [SerializeField] private Button replayButton;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button replayButtonFailure;

    [Header("Player References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private UnityEngine.InputSystem.PlayerInput playerInput;

    void Awake()
    {
        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();
        if (playerInput == null)
            playerInput = FindFirstObjectByType<UnityEngine.InputSystem.PlayerInput>();

        replayButton?.onClick.AddListener(OnReplay);
        nextLevelButton?.onClick.AddListener(OnNextLevel);
        replayButtonFailure?.onClick.AddListener(OnReplay);

        Hide();
    }

    public void ShowSuccess()
    {
        successPanel?.SetActive(true);
        failurePanel?.SetActive(false);
        gameObject.SetActive(true);
        FreezePlayer();
    }

    public void ShowFailure()
    {
        successPanel?.SetActive(false);
        failurePanel?.SetActive(true);
        gameObject.SetActive(true);
        FreezePlayer();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        successPanel?.SetActive(false);
        failurePanel?.SetActive(false);
    }

    private void OnReplay()
    {
        UnfreezePlayer();
        LevelManager.Instance?.LoadLevel(LevelManager.Instance.CurrentLevelIndex);
    }

    private void OnNextLevel()
    {
        if (LevelManager.Instance == null) return;
        int next = LevelManager.Instance.CurrentLevelIndex + 1;
        UnfreezePlayer();
        LevelManager.Instance.LoadLevel(next);
    }

    private void FreezePlayer()
    {
        if (playerController != null)
        {
            playerController.StopMovement();
            playerController.enabled = false;
        }
        playerInput?.DeactivateInput();
    }

    private void UnfreezePlayer()
    {
        if (playerController != null)
            playerController.enabled = true;
        playerInput?.ActivateInput();
    }
}
