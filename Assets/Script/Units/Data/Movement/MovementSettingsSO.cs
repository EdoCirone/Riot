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

    [Header("Skirmish")]
    [SerializeField] private float _skirmishWindupDistance = 0.3f;
    [SerializeField] private float _skirmishWindupDuration = 0.1f;
    [SerializeField] private float _skirmishEndDistance = 0.5f;
    [SerializeField] private float _skirmishAtkDuration = 0.15f;

    [Header("Bump")]
    [SerializeField] private float _chargeBumpDistance = 0.5f;
    [SerializeField] private float _chargeBumpDuration = 0.1f;
    [SerializeField] private float _skirmishBumpDistance = 0.3f;
    [SerializeField] private float _skirmishBumpDuration = 0.1f;

    [SerializeField] private float _recoilDuration = 1f;
    public float MoveDuration => _moveDuration;
    public float WindupDistance => _windupDistance;
    public float WindupDuration => _windupDuration;
    public float ChargeDuration => _chargeDuration;
    public float RecoilDuration => _recoilDuration;
    public float SkirmishWindupDistance => _skirmishWindupDistance;
    public float SkirmishWindupDuration => _skirmishWindupDuration;
    public float SkirmishEndDistance => _skirmishEndDistance;
    public float SkirmishAtkDuration => _skirmishAtkDuration;
    public float ChargeBumpDistance => _chargeBumpDistance;
    public float ChargeBumpDuration => _chargeBumpDuration;
    public float SkirmishBumpDistance => _skirmishBumpDistance;
    public float SkirmishBumpDuration => _skirmishBumpDuration;

}
