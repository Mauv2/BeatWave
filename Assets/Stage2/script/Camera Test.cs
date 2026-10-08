using UnityEngine;

public class CameraTest : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("트리거 작동!");

        // Player가 들어왔을 때만 실행하고 싶다면
        if (collision.CompareTag("Player"))
        {
            Debug.Log("Player가 트리거에 들어왔습니다!");

            // 여기에 실행할 코드 작성
        }
    }
}