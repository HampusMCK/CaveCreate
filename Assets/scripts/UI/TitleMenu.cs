using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using UnityEngine.SceneManagement;

public class TitleMenu : MonoBehaviour
{
    public GameObject MainMenu;
    public GameObject SettingsMenu;

    [Header("Main Menu UI Elements")]
    public TextMeshProUGUI seedField;

    [Header("Settings Menu UI Elements")]
    public Slider viewDistSlider;
    public TextMeshProUGUI viewDistText;
    public Slider mouseSlider;
    public TextMeshProUGUI mouseSText;
    public Toggle threadingToggle;

    Settings settings;

    private void Awake()
    {
        if (!File.Exists(Application.dataPath + "/settings.cfg"))
        {
            settings = new Settings();
            string jsonExport = JsonUtility.ToJson(settings);
            File.WriteAllText(Application.dataPath + "/settings.cfg", jsonExport);
        }
        else
        {
            string jsonImport = File.ReadAllText(Application.dataPath + "/settings.cfg");
            settings = JsonUtility.FromJson<Settings>(jsonImport);
        }
    }

    public void StartGame()
    {
        voxelData.seed = Mathf.Abs(seedField.text.GetHashCode()) / voxelData.WorldSizeInChunks;
        SceneManager.LoadScene("World", LoadSceneMode.Single);
    }

    public void EnterSettings()
    {
        viewDistSlider.value = settings.viewDistance;
        UpdateViewDstSlider();
        mouseSlider.value = settings.mouseSensitivity;
        UpdateMouseSlider();
        threadingToggle.isOn = settings.enableThreading;

        MainMenu.SetActive(false);
        SettingsMenu.SetActive(true);
    }

    public void QuitSettings()
    {
        settings.viewDistance = (int)viewDistSlider.value;
        settings.mouseSensitivity = mouseSlider.value;
        settings.enableThreading = threadingToggle.isOn;

        string jsonExport = JsonUtility.ToJson(settings);
        File.WriteAllText(Application.dataPath + "/settings.cfg", jsonExport);

        MainMenu.SetActive(true);
        SettingsMenu.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void UpdateViewDstSlider()
    {
        viewDistText.text = "View Distance: " + viewDistSlider.value;
    }

    public void UpdateMouseSlider()
    {
        mouseSText.text = "Mouse Sensitivity: " + mouseSlider.value.ToString("F1");
    }
}
