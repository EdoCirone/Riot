using UnityEngine;

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
    [SerializeField] private bool _isObjective;

    // ⚠ Riservati: dichiarati ed esposti, nessun lettore in tutto Assets/Script.
    // Sono i dati della Zona Rossa (GDD 5.6, priorità 2 del cap. 16). Non sono codice
    // morto: non cancellare. E non usarli in un punto isolato prima di implementare la
    // regola, o ti ritrovi un decimo di meccanica dove nessuno andrà a cercarla.
    [SerializeField] private bool _isRedZone;

    //two generic modifiers that can be usefull
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
