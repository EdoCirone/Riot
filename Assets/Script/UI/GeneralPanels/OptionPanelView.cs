using UnityEngine;
using UnityEngine.UI;

public class OptionPanelView : MonoBehaviour
{
    [Header("Slider")]
    [SerializeField] private Slider _masterSlider;
    [SerializeField] private Slider _musicSlider;
    [SerializeField] private Slider _sfxSlider;

    private AudioManager _audioManager;

    public void Open()
    {
        _audioManager = FindAnyObjectByType<AudioManager>();

        if(_audioManager == null)
        {
            Debug.LogWarning("Option Panel cannot find a AudioManager");
            return;
        }

        _masterSlider.SetValueWithoutNotify(_audioManager.GetGeneralVolume());
        _musicSlider.SetValueWithoutNotify(_audioManager.GetMusicVolume());
        _sfxSlider.SetValueWithoutNotify(_audioManager.GetSFXVolume());

        _masterSlider.onValueChanged.AddListener(_audioManager.SetGeneralAudio);
        _musicSlider.onValueChanged.AddListener(_audioManager.SetMusicVolume);
        _sfxSlider.onValueChanged.AddListener(_audioManager.SetSFXVolume);

    }

    public void Close()
    {
        Debug.Log("Close chiamato");

        if (_audioManager == null) return;

        _masterSlider.onValueChanged.RemoveListener(_audioManager.SetGeneralAudio);
        _musicSlider.onValueChanged.RemoveListener(_audioManager.SetMusicVolume);
        _sfxSlider.onValueChanged.RemoveListener(_audioManager.SetSFXVolume);

        _audioManager.SaveAudioSettings(_masterSlider.value, _musicSlider.value, _sfxSlider.value);
        _audioManager = null;

    }

}
