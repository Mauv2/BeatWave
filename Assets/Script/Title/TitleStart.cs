using UnityEngine;

public class TitleStart : MonoBehaviour
{
    [Header("UI")]
    public GameObject menuPanel;
    public GameObject pressText;
    public GameObject titleImage;

    private bool isStarted = false;

    void Start()
    {
        // ¸Þ´º´Â Ã³À½¿¡ ¼û±è
        if (menuPanel != null)
            menuPanel.SetActive(false);

    }

    void Update()
    {
        if (isStarted) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            isStarted = true;

            // ¸Þ´º Ç¥½Ã
            if (menuPanel != null)
                menuPanel.SetActive(true);

            // Press Text ¼û±è
            if (pressText != null)
                pressText.SetActive(false);

            // Title Image ¼û±è
            if (titleImage != null)
                titleImage.SetActive(false);
        }
    }
}