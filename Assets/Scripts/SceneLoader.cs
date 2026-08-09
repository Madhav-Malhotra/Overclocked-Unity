using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    const string MainMenuScene = "MainMenu";
    const string EndScreenScene = "EndScreen";

    public static void LoadMainMenu()
    {
        SceneManager.LoadScene(MainMenuScene);
    }

    public static void LoadGame(int levelIndex)
    {
        LevelTransferData.NextLevelIndex = levelIndex;

        string sceneName = LevelManager.Instance?.GetSceneNameForLevel(levelIndex);
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"SceneLoader: Could not resolve sceneName for level index {levelIndex}");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    public static void LoadEndScreen(bool success, float timeTaken, int completedCount, int totalCount, int nextLevelIndex)
    {
        LevelTransferData.Success = success;
        LevelTransferData.TimeTaken = timeTaken;
        LevelTransferData.CompletedCount = completedCount;
        LevelTransferData.TotalCount = totalCount;
        LevelTransferData.NextLevelIndex = nextLevelIndex;
        SceneManager.LoadScene(EndScreenScene);
    }
}
