using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapLoop : MonoBehaviour
{
    // RawImage 가져오기
    RawImage ri;

    // 배경 이동 속도
    public float scrollSpeed = 0.1f;

    void Start()
    {
        // 같은 오브젝트의 RawImage 가져오기
        ri = GetComponent<RawImage>();
    }

    void Update()
    {
        // 현재 UV 값 가져오기
        Rect uv = ri.uvRect;

        // 왼쪽으로 이동
        uv.x -= scrollSpeed * Time.deltaTime;

        // 변경된 UV 적용
        ri.uvRect = uv;
    }
}
