using UnityEngine;

public class BackgroundTrigger : MonoBehaviour
{
    [Header("변경할 배경 번호")]
    public int backgroundIndex;

    private Transform mainCamera;
    private bool activated = false;

    void Start()
    {
        if (Camera.main != null)
        {
            mainCamera = Camera.main.transform;
        }
        else
        {
            Debug.LogError("Main Camera를 찾지 못했습니다.");
        }
    }

    void Update()
    {
        if (activated)
            return;

        if (mainCamera == null)
            return;

        if (mainCamera.position.x >= transform.position.x)
        {
            activated = true;

            Debug.Log(
                "Trigger 통과! 변경할 배경 : "
                + backgroundIndex
            );

            if (BackgroundLoop.Instance != null)
            {
                BackgroundLoop.Instance.ChangeBackground(
                    backgroundIndex
                );
            }
            else
            {
                Debug.LogError(
                    "BackgroundLoop.Instance가 없습니다."
                );
            }
        }
    }
}