using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SettingsManager : MonoBehaviour
{
    [Header("Audio Controls")]
    public Slider volumeSlider;

    [Header("Graphics Controls")]
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;

    private Resolution[] resolutions;

    void Start()
    {
        // --- Audio Setup ---
        // Default volume to max or load saved preference
        float currentVolume = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
        if (volumeSlider != null)
        {
            volumeSlider.value = currentVolume;
            AudioListener.volume = currentVolume;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        // --- Graphics Setup ---
        if (resolutionDropdown != null)
        {
            resolutions = Screen.resolutions;
            resolutionDropdown.ClearOptions();

            List<string> options = new List<string>();
            int currentResolutionIndex = 0;

            for (int i = 0; i < resolutions.Length; i++)
            {
                string option = resolutions[i].width + " x " + resolutions[i].height + " @ " + resolutions[i].refreshRateRatio.value + "Hz";
                options.Add(option);

                if (resolutions[i].width == Screen.currentResolution.width &&
                    resolutions[i].height == Screen.currentResolution.height)
                {
                    currentResolutionIndex = i;
                }
            }

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = currentResolutionIndex;
            resolutionDropdown.RefreshShownValue();

            resolutionDropdown.onValueChanged.AddListener(SetResolution);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }
    }

    public void SetVolume(float sliderValue)
    {
        AudioListener.volume = sliderValue;
        PlayerPrefs.SetFloat("MasterVolume", sliderValue); // Save user preference
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }
}