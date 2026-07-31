using UnityEngine;

public class SceneMusicHandler : MonoBehaviour
{
    [SerializeField] EventMusicSO _playevent;
    [SerializeField] AudioClip _clip;

    private void Start() => _playevent?.Raise(_clip);

}
