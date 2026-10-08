using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapMover : MonoBehaviour
{
    // Start is called before the first frame update
    public float moveSpeed = 5f;

    public bool isPaused = false;

    void Update()
    {
        if (isPaused)
            return;

        transform.position += Vector3.left * moveSpeed * Time.deltaTime;
    }
}
