using UnityEngine;

[CreateAssetMenu(
    fileName = "TensionSettings",
    menuName = "RIOT/Tension/Settings"
)]
public sealed class TensionSettingsSO : ScriptableObject
{
    [Header("Global tension events")]

    [Min(0)]
    [SerializeField] private int _objectiveEntry = 10;

    [Min(0)]
    [SerializeField] private int _playerInitiatedSkirmish = 10;

    [Min(0)]
    [SerializeField] private int _violentCharge = 20;

    public int ObjectiveEntry =>
        _objectiveEntry;

    public int PlayerInitiatedSkirmish =>
        _playerInitiatedSkirmish;

    public int ViolentCharge =>
        _violentCharge;
}
