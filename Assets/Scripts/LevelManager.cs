using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [SerializeField] private TextAsset[] levelJsonFiles;

    [SerializeField] private PlayerController playerController;
    [SerializeField] private UnityEngine.InputSystem.PlayerInput playerInput;

    private LevelData currentLevelData;
    private Queue<InstructionData> instructionQueue = new();
    private int completedCount;
    private int totalCount;
    private float timeRemaining;
    private bool levelActive;
    private int currentLevelIndex;

    public float TimeRemaining => timeRemaining;
    public int CompletedCount => completedCount;
    public int TotalCount => totalCount;
    public int CurrentLevelIndex => currentLevelIndex;
    public bool LevelActive => levelActive;

void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        FindPlayerReferences();
        LoadLevel(LevelTransferData.NextLevelIndex);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Playground") return;

        FindPlayerReferences();
        LoadLevel(LevelTransferData.NextLevelIndex);
    }

    private void FindPlayerReferences()
    {
        playerController = FindFirstObjectByType<PlayerController>();
        playerInput = FindFirstObjectByType<UnityEngine.InputSystem.PlayerInput>();
    }

    void Update()
    {
        if (!levelActive) return;

        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            OnTimeLimitReached();
        }
    }

public void LoadLevel(int index)
    {
        if (levelJsonFiles == null || index < 0 || index >= levelJsonFiles.Length)
        {
            Debug.LogError($"LevelManager: No level JSON at index {index}");
            return;
        }

        currentLevelIndex = index;
        currentLevelData = JsonUtility.FromJson<LevelData>(levelJsonFiles[index].text);

        instructionQueue.Clear();
        if (currentLevelData.instructions != null)
        {
            foreach (InstructionData instr in currentLevelData.instructions)
                instructionQueue.Enqueue(instr);
        }

        totalCount = instructionQueue.Count;
        completedCount = 0;
        timeRemaining = currentLevelData.timeLimit;
        levelActive = true;

        UnfreezePlayer();

        StartPlatform startPlatform = FindFirstObjectByType<StartPlatform>();
        startPlatform?.SpawnNextInstruction();
    }

    public InstructionData GetNextInstruction()
    {
        if (instructionQueue.Count == 0) return null;
        return instructionQueue.Dequeue();
    }

    public bool HasMoreInstructions()
    {
        return instructionQueue.Count > 0;
    }

    public void OnInstructionCompleted()
    {
        completedCount++;
        if (completedCount >= totalCount)
            OnLevelSuccess();
    }

private void OnLevelSuccess()
    {
        levelActive = false;
        FreezePlayer();
        float timeTaken = currentLevelData.timeLimit - timeRemaining;
        SceneLoader.LoadEndScreen(true, timeTaken, completedCount, totalCount, currentLevelIndex);
    }

private void OnTimeLimitReached()
    {
        levelActive = false;
        FreezePlayer();
        SceneLoader.LoadEndScreen(false, 0f, completedCount, totalCount, currentLevelIndex);
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
