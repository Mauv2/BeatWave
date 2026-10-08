using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class JudgeTextPopup : MonoBehaviour
{
    public float moveSpeed = 1.0f;
    public float lifeTime = 0.7f;

    private TextMeshPro textMesh;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
    }

    public void SetText(string message)
    {
        if (textMesh == null)
            textMesh = GetComponent<TextMeshPro>();

        textMesh.text = message;
    }

    void Update()
    {
        // 위로 천천히 이동
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        // 일정 시간 후 삭제
        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
