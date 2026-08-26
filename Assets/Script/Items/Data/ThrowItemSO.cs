using UnityEngine;

[CreateAssetMenu(fileName = "ThrowItemSO", menuName = "RIOT/Items/ThrowItemSO")]
public class ThrowItemSO : ItemSO
{
    [SerializeField] private int _moralLost;

    [Min(0)]
    [SerializeField] private int _tensionImpact;

    public override ActionType Action => ActionType.Throw;
    public int MoralLost => _moralLost;
    public int TensionImpact => _tensionImpact;
}
