using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject mainButtonsPanel;
    public GameObject settingsPanel;
    public GameObject audioPanel;
    public GameObject graphicsPanel;

    [Header("Level Settings")]
    public string gameSceneName = "SampleScene";

    public void PlayGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenSettings()
    {
        mainButtonsPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        mainButtonsPanel.SetActive(true);
    }

    public void OpenAudioSettings()
    {
        settingsPanel.SetActive(false);
        audioPanel.SetActive(true);
    }

    public void CloseAudioSettings()
    {
        audioPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void OpenGraphicsSettings()
    {
        settingsPanel.SetActive(false);
        graphicsPanel.SetActive(true);
    }

    public void CloseGraphicsSettings()
    {
        graphicsPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    // --- Quit ---
    public void QuitGame()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}