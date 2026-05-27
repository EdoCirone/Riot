using UnityEngine;

[CreateAssetMenu(fileName = "HexTypeSO", menuName = "MyData/Maps/HexTypeSO")]

public class HexTypeSO : ScriptableObject
{
    [Header("Info")]
    [SerializeField] private string _displayType;
    [SerializeField] private string _id;

    [Header("Visuals")]
    [SerializeField] private GameObject _prefab;
    [SerializeField] private Color _color;

    [Header("Properties")]
    [SerializeField] private bool _isWalkable;
    [SerializeField] private bool _isObjective;
    [SerializeField] private bool _isRedZone;

    //Uso due modificatori generici per adesso, così che se volessi potrei definire il modificatore A modifica dell'attacco e il B modifica difesa, ma anche modificatore A modifica morale modificatore B modifica iniziativa, se poi mi accorgo di dover usare più modificatori ne aggiungo se non mi servono li lascio a 0 e tanti saluti
    [Header("Modifiers")]
    [SerializeField] int _modifierA;
    [SerializeField] private int _modifierB;

    public string DisplayType => _displayType;
    public string Id => _id;

    public GameObject Prefab => _prefab;
    public Color Color => _color;

    public bool IsWalkable => _isWalkable;
    public bool IsObjective => _isObjective;

    public bool IsRedZone => _isRedZone;
    public int ModifierA => _modifierA;
    public int ModifierB => _modifierB;
}
