using UnityEngine;

[CreateAssetMenu(fileName = "ThrowItemSO", menuName = "RIOT/Items/ThrowItemSO")]

public class ThrowItemSO : ScriptableObject
{
    [Header("General")]
    [SerializeField] private string _name;
    [SerializeField] private string _id;

    [Header ("Graphic")]
    [SerializeField] private Sprite _inventoryIcon;
    [SerializeField] private GameObject _graphicPrefab;

    [Header("Settings")]
    [SerializeField] private int _actionPointCost;
    [SerializeField] private int _moralLost;

    public string Name => _name;
    public string Id => _id;
    public int ActionPointCost => _actionPointCost;
    public int MoralLost => _moralLost;
}
