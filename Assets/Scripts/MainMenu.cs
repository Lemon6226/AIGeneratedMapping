using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenu : MonoBehaviour
{
    [Header("Panely")]
    [SerializeField] GameObject mainPanel;
    [SerializeField] GameObject settingsPanel;
    [SerializeField] GameObject generateMapPanel;

    [Header("Generování mapy")]
    [SerializeField] Slider   spawnerSlider;
    [SerializeField] TMP_Text spawnerLabel;
    [SerializeField] Slider   itemSlider;
    [SerializeField] TMP_Text itemLabel;

    [Header("Nastavení")]
    [SerializeField] Slider   masterVolumeSlider;
    [SerializeField] TMP_Text masterVolumeLabel;
    [SerializeField] Slider   mouseSensSlider;
    [SerializeField] TMP_Text mouseSensLabel;

    [Header("Scéna")]
    [SerializeField] int gameSceneIndex = 1;

    // klíče pro uložení nastavení mezi spuštěními hry
    const string KEY_VOL  = "MasterVolume";
    const string KEY_SENS = "MouseSens";

    void Start()
    {
        ShowPanel(mainPanel);

        // načtení uloženého nastavení
        float vol  = PlayerPrefs.GetFloat(KEY_VOL,  1f);
        float sens = PlayerPrefs.GetFloat(KEY_SENS, 0.5f);

        if (masterVolumeSlider)
        {
            masterVolumeSlider.value = vol;
            masterVolumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            UpdateVolumeLabel(vol);
        }
        if (mouseSensSlider)
        {
            mouseSensSlider.value = sens;
            mouseSensSlider.onValueChanged.AddListener(OnSensChanged);
            UpdateSensLabel(sens);
        }
        if (spawnerSlider)
        {
            spawnerSlider.onValueChanged.AddListener(_ => UpdateSpawnerLabel());
            UpdateSpawnerLabel();
        }
        if (itemSlider)
        {
            itemSlider.onValueChanged.AddListener(_ => UpdateItemLabel());
            UpdateItemLabel();
        }

        AudioListener.volume = vol;
    }

    public void OnPlayClicked()
    {
        PersistentMapConfig.Reset();
        SceneManager.LoadScene(gameSceneIndex);
    }

    public void OnGenerateMapClicked()  => ShowPanel(generateMapPanel);
    public void OnSettingsClicked()     => ShowPanel(settingsPanel);

    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnGenerateAndPlayClicked()
    {
        // uložení nastavení mapy a spuštění hry
        PersistentMapConfig.HasCustomConfig = true;
        PersistentMapConfig.SpawnerCount    = spawnerSlider ? Mathf.RoundToInt(spawnerSlider.value) : 2;
        PersistentMapConfig.ItemCount       = itemSlider    ? Mathf.RoundToInt(itemSlider.value)    : 3;
        SceneManager.LoadScene(gameSceneIndex);
    }

    public void OnGenerateMapBackClicked() => ShowPanel(mainPanel);

    public void OnSettingsBackClicked()
    {
        PlayerPrefs.Save();
        ShowPanel(mainPanel);
    }

    void OnVolumeChanged(float v) { AudioListener.volume = v; PlayerPrefs.SetFloat(KEY_VOL, v); UpdateVolumeLabel(v); }
    void OnSensChanged(float v)   { PlayerPrefs.SetFloat(KEY_SENS, v); UpdateSensLabel(v); }

    void UpdateVolumeLabel(float v)  { if (masterVolumeLabel) masterVolumeLabel.text = $"Hlasitost: {Mathf.RoundToInt(v * 100)}%"; }
    void UpdateSensLabel(float v)    { if (mouseSensLabel)    mouseSensLabel.text    = $"Citlivost: {v:F2}"; }
    void UpdateSpawnerLabel()        { if (spawnerLabel)      spawnerLabel.text      = $"Spawnery: {Mathf.RoundToInt(spawnerSlider.value)}"; }
    void UpdateItemLabel()           { if (itemLabel)         itemLabel.text         = $"Itemy: {Mathf.RoundToInt(itemSlider.value)}"; }

    void ShowPanel(GameObject target)
    {
        if (mainPanel)        mainPanel.SetActive(mainPanel == target);
        if (settingsPanel)    settingsPanel.SetActive(settingsPanel == target);
        if (generateMapPanel) generateMapPanel.SetActive(generateMapPanel == target);
    }
}
