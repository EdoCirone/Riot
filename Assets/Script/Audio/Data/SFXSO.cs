using UnityEngine;
using UnityEngine.LightTransport;

[CreateAssetMenu(fileName = "SFXSO", menuName = "RIOT/Audio/SFXSO")]
public class SFXSO : ScriptableObject
{
    [Header ("Trigger")]
    [SerializeField] GameEventSO _soundEvent;

    [Header("Clips")]
    [SerializeField] AudioClip[] _clips;

    [Header("Variations")]
    [SerializeField] float _pitchMin = 0.95f;
    [SerializeField] float _pitchMax = 1.0f;

    private int _lastIndex = -1;
    public GameEventSO SoundEvent => _soundEvent;

    public void Play(AudioSource source)
    {
        if (source == null) return;
        if(_clips == null|| _clips.Length == 0)
        {
            Debug.LogWarning($"[SFX] {name}: nessuna clip assegnata");
            return;
        }
        source.pitch = Random.Range( _pitchMin, _pitchMax );
        source.PlayOneShot(PickClip());
    }

    private AudioClip PickClip()
    {
        if (_clips.Length == 1) return _clips[0];

        int index;
        
        do { index = Random.Range(0, _clips.Length); }
        while (index == _lastIndex);

        _lastIndex = index;
        
        return _clips[index];
    }
}
