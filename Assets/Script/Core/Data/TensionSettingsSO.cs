using UnityEngine;

[CreateAssetMenu(fileName = "TensionSettings", menuName = "RIOT/Tension/Settings")]
public sealed class TensionSettingsSO : ScriptableObject
{
    [Header("Global tension events")]

    [Min(0)]
    [SerializeField] private int _objectiveEntry = 10;

    [Min(0)]
    [SerializeField] private int _playerInitiatedSkirmish = 10;

    [Min(0)]
    [SerializeField] private int _violentCharge = 20;

    [Header("Police leash by tension band")]

    [Tooltip("Maximum distance from the guarded objective during Containment.")]
    [Min(0)]
    [SerializeField] private int _containmentLeashRadius = 4;

    [Tooltip("Maximum distance from the guarded objective during Engage.")]
    [Min(0)]
    [SerializeField] private int _engageLeashRadius = 8;

    public int ObjectiveEntry => _objectiveEntry;

    public int PlayerInitiatedSkirmish => _playerInitiatedSkirmish;

    public int ViolentCharge => _violentCharge;

    public int GetLeashRadius(EngagementRules rules)
    {
        return rules switch
        {
            EngagementRules.Containment =>
                _containmentLeashRadius,

            EngagementRules.Engage =>
                _engageLeashRadius,

            // Sweep ignores the leash in PoliceAI.
            EngagementRules.Sweep =>
                _engageLeashRadius,

            _ => _containmentLeashRadius
        };
    }
}
