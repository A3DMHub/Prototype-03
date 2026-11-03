using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverController : MonoBehaviour
{
    [Header("Scene Navigation")]
    public string gameplaySceneName = "TestLevel"; // Change to match your exact gameplay scene name
    public string mainMenuSceneName = "MainMenuScene"; // Change to match your main menu scene name

    public void RetryGame()
    {
        Time.timeScale = 1f; // Unfreeze time
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f; // Unfreeze time
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}