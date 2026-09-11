using UnityEngine;

[CreateAssetMenu(
    fileName = "SpezzoneSO",
    menuName = "RIOT/Units/SpezzoneSO")]
public class SpezzoneSO : UnitsSO
{
    [Header("Assembly")]
    [Min(1)]
    [SerializeField] private int _activationCost = 3;

    public int ActivationCost => _activationCost;
}
