
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameAudioManager : MonoBehaviour
{
    public static GameAudioManager Instance;

    [SerializeField] private AudioMixer audioMixer;

    private const string VolumeKey = "MasterVolume";
    [System.Serializable] private class Level { public string name; public float gain; }
    [System.Serializable] private class Levels { public Level[] entries; }
    private static Dictionary<string, float> clipLevels;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeAudio()
    {
        clipLevels = null;
        AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        SceneManager.sceneLoaded -= ConfigureScene;
        SceneManager.sceneLoaded += ConfigureScene;
    }

    private static void ConfigureScene(Scene scene, LoadSceneMode mode)
    {
        foreach (var root in scene.GetRootGameObjects())
            foreach (var source in root.GetComponentsInChildren<AudioSource>(true))
                ConfigureSource(source);
    }

    public static float ClipVolume(AudioClip clip)
    {
        if (clipLevels == null)
        {
            clipLevels = new Dictionary<string, float>();
            var data = Resources.Load<TextAsset>("AudioLevels");
            if (data != null)
                foreach (var level in JsonUtility.FromJson<Levels>(data.text).entries)
                    clipLevels[level.name] = level.gain;
        }
        return clip != null && clipLevels.TryGetValue(clip.name, out var gain) ? gain : 1f;
    }

    public static void ConfigureSource(AudioSource source)
    {
        if (source == null) return;
        // Master is applied by the listener, including sources without a mixer.
        if (source.outputAudioMixerGroup != null)
            source.outputAudioMixerGroup.audioMixer.SetFloat(VolumeKey, 0f);
        source.volume = ClipVolume(source.clip);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        float savedVolume =
            PlayerPrefs.GetFloat(VolumeKey, 1f);

        ApplyVolume(savedVolume);
    }

    public void SetVolume(float value)
    {
        value = Mathf.Clamp01(value);

        ApplyVolume(value);

        PlayerPrefs.SetFloat(VolumeKey, value);
        PlayerPrefs.Save();
    }

    private void ApplyVolume(float value)
    {
        AudioListener.volume = Mathf.Clamp01(value);
        // Avoid multiplying the same master setting twice.
        if (audioMixer != null) audioMixer.SetFloat(VolumeKey, 0f);
    }

    public float GetVolume()
    {
        return PlayerPrefs.GetFloat(VolumeKey, 1f);
    }
}
