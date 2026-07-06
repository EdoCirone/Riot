using UnityEngine;

public abstract class ItemSO : ScriptableObject
{
    [Header("General")]
    [SerializeField] protected string _name;
    [SerializeField] protected string _id;
    [Header("Graphic")]
    [SerializeField] protected Sprite _inventoryIcon;
    [SerializeField] protected GameObject _graphicPrefab;
    [Header("Settings")]
    [SerializeField] protected int _actionPointCost;

    public abstract ActionType Action { get; }

    public string Name => _name;
    public string Id => _id;
    public Sprite InventoryIcon => _inventoryIcon;
    public GameObject GraphicPrefab => _graphicPrefab;
    public int ActionPointCost => _actionPointCost;
}