using System.Collections.Generic;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
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



    private void Awake()
    {


        if (_audioMixer == null)
        {
            Debug.LogWarning("Set the mixer");
            return;
        }

        if (_musicSource == null && _sfxSource == null)
        {
            Debug.LogWarning("Set the audioSources");
            return;
        }

        DontDestroyOnLoad(this);
    }

  
    private void OnEnable()
    {
        _playMusicEvent.Subscribe(OnPlayMusic);
    }

    private void OnDisable()
    {
        _playMusicEvent.Unsubscribe(OnPlayMusic);
    }
    #region VolumeSettings

    public void SetGeneralAudio(float value)
    {
        _audioMixer.SetFloat("VolumeMaster", Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f);
    }

    public void SetMusicVolume(float value)
    {
        _audioMixer.SetFloat("VolumeMusic", Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f);
    }

    public void SetSFXVolume(float value)
    {
        _audioMixer.SetFloat("VolumeSFX", Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f);
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
    public void SaveAudioSettings(float masterVolume, float musicVolume, float sfxVolume)
    {
        PlayerPrefs.SetFloat("VolumeMaster", masterVolume);
        PlayerPrefs.SetFloat("VolumeMusic", musicVolume);
        PlayerPrefs.SetFloat("VolumeSFX", sfxVolume);
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
    #endregion
}
