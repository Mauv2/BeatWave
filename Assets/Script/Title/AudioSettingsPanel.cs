
using UnityEngine;

public class AudioSettingsPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject audioSettingsPanel;

    public void OpenSettings()
    {
        audioSettingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        audioSettingsPanel.SetActive(false);
    }
}
