using UnityEngine;

[CreateAssetMenu(fileName = "SFXSO", menuName = "RIOT/Audio/SFXSO")]
public class SFXSO : ScriptableObject
{
    [SerializeField] GameEventSO _soundEvent;
    [SerializeField] AudioClip _clip;

    public GameEventSO SoundEvent => _soundEvent;
    public AudioClip Clip => _clip;
}
