using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class NoteMove : MonoBehaviour
{
    public float speed = 10.0f;
    public bool isStopped = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(isStopped) return;

        PlayNote playNote = FindObjectOfType<PlayNote>();

        if (playNote != null && playNote.isNoteFlowPaused)
        {
            return;
        }

        transform.position += Vector3.left * speed * Time.deltaTime;
    }
}
