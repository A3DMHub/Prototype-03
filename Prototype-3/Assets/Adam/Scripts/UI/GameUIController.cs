using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GameUIController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject pauseMenuPanel;
    public GameObject settingsPanel;
    public GameObject audioPanel;
    public GameObject graphicsPanel;
    public GameObject gameOverPanel;
    public GameObject victoryPanel;

    [Header("Scene Settings")]
    public string mainMenuSceneName = "MainMenuScene";

    private bool isPaused = false;

    void Update()
    {
        // Check for Escape key press (supports legacy and new Input System)
        bool escapePressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            escapePressed = true;
#else
        if (Input.GetKeyDown(KeyCode.Escape))
            escapePressed = true;
#endif

        if (escapePressed)
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // Freeze game physics and timer

        // Ensure panels open cleanly
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (audioPanel != null) audioPanel.SetActive(false);
        if (graphicsPanel != null) graphicsPanel.SetActive(false);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // Resume game

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (audioPanel != null) audioPanel.SetActive(false);
        if (graphicsPanel != null) graphicsPanel.SetActive(false);
    }

    // --- Settings Navigation ---
    public void OpenSettings()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettingsFromPause()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
    }

    public void OpenAudioSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (audioPanel != null) audioPanel.SetActive(true);
    }

    public void CloseAudioSettings()
    {
        if (audioPanel != null) audioPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void OpenGraphicsSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (graphicsPanel != null) graphicsPanel.SetActive(true);
    }

    public void CloseGraphicsSettings()
    {
        if (graphicsPanel != null) graphicsPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    // --- Game States ---
    public void TriggerGameOver()
    {
        Time.timeScale = 0f;
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    public void TriggerVictory()
    {
        Time.timeScale = 0f;
        if (victoryPanel != null) victoryPanel.SetActive(true);
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
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