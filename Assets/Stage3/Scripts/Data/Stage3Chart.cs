using System;
namespace BeatWave.Stage3
{
    [Serializable] public class Stage3Note { public float time; public int lane; public float duration; }
    [Serializable] public class Stage3Chart { public string title; public float bpm, offset, duration; public Stage3Note[] notes; }
}
