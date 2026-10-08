using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundFollowCamera : MonoBehaviour
{
    public Transform targetCamera;

    void LateUpdate()
    {
        if (targetCamera == null)
            return;

        transform.position = new Vector3(
            targetCamera.position.x,
            targetCamera.position.y,
            transform.position.z
        );
    }
}
