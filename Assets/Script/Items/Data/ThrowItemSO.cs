using UnityEngine;

[CreateAssetMenu(fileName = "ThrowItemSO", menuName = "RIOT/Items/ThrowItemSO")]

public class ThrowItemSO : ItemSO
{
    [SerializeField] private int _moralLost;

    public int MoralLost => _moralLost;
}
