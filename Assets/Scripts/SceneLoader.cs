using UnityEngine.SceneManagement;

public static class SceneLoader
{
    const string MainMenuScene = "MainMenu";
    const string GameScene = "Playground";
    const string EndScreenScene = "EndScreen";

    public static void LoadMainMenu()
    {
        SceneManager.LoadScene(MainMenuScene);
    }

    public static void LoadGame(int levelIndex)
    {
        LevelTransferData.NextLevelIndex = levelIndex;
        SceneManager.LoadScene(GameScene);
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
