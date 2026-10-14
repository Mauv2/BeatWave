using UnityEngine;

public class BackgroundLoop : MonoBehaviour
{
    public static BackgroundLoop Instance;

    [Header("배경 오브젝트")]
    public SpriteRenderer background1;
    public SpriteRenderer background2;

    [Header("배경 이미지")]
    public Sprite[] backgrounds;

    [Header("메인 카메라")]
    public Camera mainCamera;

    [Header("배경 이동 속도")]
    public float scrollSpeed = 0.5f;

    private float backgroundWidth;
    private float cameraHalfWidth;

    // 현재 사용할 배경 이미지 번호
    private int currentBackground = 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        // 처음 배경
        background1.sprite = backgrounds[0];
        background2.sprite = backgrounds[0];

        // 배경 너비
        backgroundWidth = background1.bounds.size.x;

        // 카메라 가로 절반
        cameraHalfWidth =
            mainCamera.orthographicSize * mainCamera.aspect;

        // 두 번째 배경을 첫 번째 배경 오른쪽에 배치
        background2.transform.position =
            background1.transform.position +
            Vector3.right * backgroundWidth;
    }

    void Update()
    {
        MoveBackground();
        CheckLoop();
    }

    void MoveBackground()
    {
        float move = scrollSpeed * Time.deltaTime;

        background1.transform.position += Vector3.left * move;
        background2.transform.position += Vector3.left * move;
    }

    void CheckLoop()
    {
        float cameraLeft =
            mainCamera.transform.position.x - cameraHalfWidth;

        // background1이 카메라 왼쪽으로 완전히 사라짐
        if (background1.bounds.max.x < cameraLeft)
        {
            // 오른쪽으로 이동
            background1.transform.position = new Vector3(
                background2.bounds.max.x + backgroundWidth / 2f,
                background1.transform.position.y,
                background1.transform.position.z
            );

            // ★ 새로운 배경 적용
            background1.sprite = backgrounds[currentBackground];

            Debug.Log(
                "Background1 재배치 / 현재 이미지 번호 : "
                + currentBackground
            );
        }

        // background2가 카메라 왼쪽으로 완전히 사라짐
        if (background2.bounds.max.x < cameraLeft)
        {
            // 오른쪽으로 이동
            background2.transform.position = new Vector3(
                background1.bounds.max.x + backgroundWidth / 2f,
                background2.transform.position.y,
                background2.transform.position.z
            );

            // ★ 새로운 배경 적용
            background2.sprite = backgrounds[currentBackground];

            Debug.Log(
                "Background2 재배치 / 현재 이미지 번호 : "
                + currentBackground
            );
        }
    }

    public void ChangeBackground(int index)
    {
        if (index < 0 || index >= backgrounds.Length)
        {
            Debug.LogError(
                "잘못된 배경 번호 : " + index
            );

            return;
        }

        currentBackground = index;

        Debug.Log(
            "배경 변경 예약 성공! 다음 배경 번호 : "
            + currentBackground
        );
    }
}