using UnityEngine;

[CreateAssetMenu(fileName = "PoliceSO", menuName = "RIOT/Units/PoliceSO")]
public class PoliceSO : UnitsSO
{
    [Header("Redeployment")]
    [Tooltip("Complete police turns spent off-board after dispersion.")]
    [Min(1)]
    [SerializeField] private int _redeployTurns = 1;

    public int RedeployTurns =>
        Mathf.Max(1, _redeployTurns);
}
