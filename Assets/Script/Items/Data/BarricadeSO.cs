using UnityEngine;

[CreateAssetMenu(fileName = "BarricadeSO", menuName = "RIOT/Items/BarricadeSO")]
public class BarricadeSO : ScriptableObject
{
    [Header("General")]
    [SerializeField] private string _name;
    [SerializeField] private string _id;

    [Header("Settings")]
    [SerializeField] private int _actionPointCost;

    [Header("Graphic")]
    [SerializeField] private Sprite _inventoryIcon;

    [SerializeField] private GameObject _GraphicPrefab;

    public string Name => _name;
    public string Id => _id;

    public int ActionPointCost => _actionPointCost;

    public Sprite InventoryIcon => _inventoryIcon;
    public GameObject GraphicPrefab => _GraphicPrefab;



}
