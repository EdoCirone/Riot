using UnityEngine;

[CreateAssetMenu(fileName = "MovementSettingsSO", menuName = ("RIOT/Anim/AnimationSettingsSO"))]
public class MovementSettingsSO : ScriptableObject
{
    [Header("Movement")]
    [SerializeField] private float _moveDuration = 1f;

    [Header("Charge")]
    [SerializeField] private float _windupDistance = 1f;
    [SerializeField] private float _windupDuration = 1f;
    [SerializeField] private float _chargeDuration = 1f;


    [SerializeField] private float _bumpDistance = 1f;
    [SerializeField] private float _bumpDuration = 1f;

    [SerializeField] private float _recoilDuration = 1f;

    public float MoveDuration => _moveDuration;

    public float WindupDistance => _windupDistance;
    public float WindupDuration => _windupDuration;
    public float ChargeDuration => _chargeDuration;
    public float BumpDistance => _bumpDistance;

    public float BumpDuration => _bumpDuration;

    public float RecoilDuration => _recoilDuration;

}
