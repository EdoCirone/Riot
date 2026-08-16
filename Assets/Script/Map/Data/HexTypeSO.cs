using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "HexTypeSO", menuName = "RIOT/Maps/HexTypeSO")]

public class HexTypeSO : ScriptableObject
{
    [Header("Info")]
    [SerializeField] private string _displayType;
    [SerializeField] private string _id;

    [Header("Visuals")]
    [SerializeField] private GameObject _prefab;
    [SerializeField] private Color _color = Color.white; // using a default value to have alpha to 100

    [Header("Properties")]
    [SerializeField] private bool _isWalkable;

    [FormerlySerializedAs("_isObjective")]
    [SerializeField] private bool _isObjectiveGround;

    [SerializeField] private bool _isRedZone;

    [SerializeField] private bool _isMeetingGround;

    //two generic modifiers that can be usefull
    [Header("Modifiers")]
    [SerializeField] int _modifierA;
    [SerializeField] private int _modifierB;


    public string DisplayType => _displayType;
    public string Id => _id;

    public GameObject Prefab => _prefab;
    public Color Color => _color;

    public bool IsWalkable => _isWalkable;
    public bool IsObjectiveGround => _isObjectiveGround;
    public bool IsMeetingGround => _isMeetingGround;

    public bool IsRedZone => _isRedZone;
    public int ModifierA => _modifierA;
    public int ModifierB => _modifierB;
}