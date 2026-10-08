using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using LitJson;

public class CreateNote : MonoBehaviour
{
    string path;

    [Header("JSON Save")]
    public string jsonFileName = "notedata.json";

    public List<NoteData> listNoteData = new List<NoteData>();

    public int mashNeedHitCount = 5;
    public double mashLimitTime = 2.0f;
    public float holdTime = 1.5f;

    JsonData noteData;
    AudioSource aus;
    void Start()
    {

        aus = GetComponent<AudioSource>();      
    }

    public void ReadNoteJsonLoad()
    {
       
        string loadJson = File.ReadAllText(path);
        JsonData noteData = JsonMapper.ToObject(loadJson);
    }

    bool bMusicStart = false;
    // float currentTime = 0.0f;
    public void StartNote()
    {
        //음악재생
        bMusicStart = true;
        aus.Play();
        path = Path.Combine(Application.dataPath, jsonFileName);
        
    }

    public void EndNote()
    {
        //음악재생
        bMusicStart = false;
        string json = JsonMapper.ToJson(listNoteData);
        File.WriteAllText(path, json);
        aus.Stop();
    }

    //public void SaveNote()
    //{
    //    NoteData note = new NoteData();
    //    note.noteType = Random.Range(0, 5);
    //    note.noteKind = 0; // 0 = 일반 노트
    //    note.createTime = aus.time;
    //    print(note);

    //    listNoteData.Add(note);
    //}

    public void SaveTapNote()
    {
        NoteData note = new NoteData();

        note.noteType = Random.Range(0, 2);
        note.noteKind = 0;
        note.createTime = aus.time;

        note.needHitCount = 0;
        note.holdTime = 0;

        Debug.Log(note.noteKind);

        listNoteData.Add(note);
    }

    public void SaveHoldNote()
    {
        NoteData note = new NoteData();

        note.noteType = Random.Range(0, 2);
        note.noteKind = 1; // Hold
        note.createTime = aus.time;

        note.holdTime = holdTime;

        note.needHitCount = 0;
        note.mashLimitTime = 0;

        Debug.Log(note.noteKind);

        listNoteData.Add(note);
    }

    public void SaveMashNote()
    {
        NoteData note = new NoteData();

        note.noteType = Random.Range(0, 2);
        note.noteKind = 2;
        note.createTime = aus.time;

        note.needHitCount = mashNeedHitCount;
        note.mashLimitTime = mashLimitTime;
        note.holdTime = 0;

        Debug.Log(note.noteKind);

        listNoteData.Add(note);
    }
}

public struct NoteData
{
    public int noteType;
    public int noteKind;
    public double createTime;

    public double holdTime;

    public int needHitCount;
    public double mashLimitTime;

}