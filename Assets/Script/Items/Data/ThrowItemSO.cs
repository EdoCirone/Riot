using UnityEngine;

[CreateAssetMenu(fileName = "ThrowItemSO", menuName = "RIOT/Items/ThrowItemSO")]

public class ThrowItemSO : ItemSO
{
    [SerializeField] private int _moralLost;

    public override ActionType Action => ActionType.Throw;
    public int MoralLost => _moralLost;
}
