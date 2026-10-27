
using UnityEngine;

public class PauseAudioSettingsUI : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject audioSettingsPanel;

    public void OpenAudioSettings()
    {
        pausePanel.SetActive(false);
        audioSettingsPanel.SetActive(true);
    }

    public void CloseAudioSettings()
    {
        audioSettingsPanel.SetActive(false);
        pausePanel.SetActive(true);
    }
}
