using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine;
public class AudioManager : MonoBehaviour
{
    [Header ("Reference")]
    [SerializeField] AudioMixer _audioMixer;
    [Space]
    [SerializeField] AudioSource _musicSource;
    [SerializeField] AudioSource _sfxSource;

    [Header("Events")]
    [SerializeField] SFXSO[] _sfxevents;
    [SerializeField] EventMusicSO _playMusicEvent;

    private bool _isValid;

    private readonly List<(GameEventSO evt, System.Action handler)> _sfxHandlers = new();

    private void Awake()
    {

        if (_audioMixer == null)
        {
            Debug.LogWarning("Set the mixer");
            return;
        }

        if (_musicSource == null || _sfxSource == null)
        {
            Debug.LogWarning("Set the audioSources");
            return;
        }

        if (_playMusicEvent == null) { Debug.LogWarning("Set the music event"); return; }

        DontDestroyOnLoad(this);
        LoadAudioSettings();
        _isValid = true;
    }

    private void OnEnable()
    {
        if (!_isValid) return;
        _playMusicEvent.Subscribe(OnPlayMusic);
        SceneManager.sceneLoaded += OnSceneLoaded;

        foreach (SFXSO sfx in _sfxevents)
        {
            if (sfx == null || sfx.SoundEvent == null) continue;
            System.Action handler = () => sfx.Play(_sfxSource);
            sfx.SoundEvent.Subscribe(handler);
            _sfxHandlers.Add((sfx.SoundEvent, handler));
        }
    }

    private void OnDisable()
    {
        if(!_isValid) return;

        _playMusicEvent.Unsubscribe(OnPlayMusic);
        SceneManager.sceneLoaded -= OnSceneLoaded;

        foreach (var (evt, handler) in _sfxHandlers)
            evt.Unsubscribe(handler);
        _sfxHandlers.Clear();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        LoadAudioSettings();
    }
    #region VolumeSettings

    public void SetGeneralAudio(float value)
    {
        _audioMixer.SetFloat("VolumeMaster", Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f);
        PlayerPrefs.SetFloat("VolumeMaster", value);
    }

    public void SetMusicVolume(float value)
    {
        _audioMixer.SetFloat("VolumeMusic", Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f);
        PlayerPrefs.SetFloat("VolumeMusic", value);
    }

    public void SetSFXVolume(float value)
    {
        _audioMixer.SetFloat("VolumeSFX", Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f);
        PlayerPrefs.SetFloat("VolumeSFX", value);
    }
    #endregion

    private void OnPlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        _musicSource.clip = clip;
        _musicSource.loop = true;
        _musicSource.Play();
    }

    #region Save & Load
    public void SaveAudioSettings()
    {
        PlayerPrefs.Save();
    }

    public void LoadAudioSettings()
    {
        float masterVolume = PlayerPrefs.GetFloat("VolumeMaster", 0.75f);
        float musicVolume = PlayerPrefs.GetFloat("VolumeMusic", 0.75f);
        float sfxVolume = PlayerPrefs.GetFloat("VolumeSFX", 0.75f);
        SetGeneralAudio(masterVolume);
        SetMusicVolume(musicVolume);
        SetSFXVolume(sfxVolume);
    }

    private float GetLinearVolume(string parameterName)
    {
        if (_audioMixer.GetFloat(parameterName, out float db))
            return Mathf.Pow(10f, db / 20f);
        return 1f;
    }

    public float GetGeneralVolume() => GetLinearVolume("VolumeMaster");
    public float GetMusicVolume() => GetLinearVolume("VolumeMusic");
    public float GetSFXVolume() => GetLinearVolume("VolumeSFX");

    #endregion
}
