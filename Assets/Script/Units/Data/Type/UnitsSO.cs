using UnityEngine;

public class UnitsSO : ScriptableObject
{
    [Header("Unit Info")]
    [SerializeField] string _id;
    [SerializeField] string _displayName;
    [Space]

    [Header("Unit Graph")]
    [SerializeField] Sprite _avatar;
    [SerializeField] GameObject _graphicsPrefab;
    [Space]

    [Header("Unit Stats")]
    [SerializeField] int _atk;
    [SerializeField] int _def;
    [SerializeField] int _mor;
    [SerializeField] int _actionPoints;
    [Space]

    [Header("Unit Aura")]
    [Tooltip("The aura is a passive effect that affects its allies within a certain range.")]
    [SerializeField] private int _auraAtk;
    [SerializeField] private int _auraDef;
    [SerializeField] private int _auraMor;
    [Space]

    [Header("Unit Actions")]
    [SerializeField] private ActionType _allowedActions;

    public string Id => _id;
    public string DisplayName => _displayName;

    public Sprite Avatar => _avatar;
    public GameObject GraphicsPrefab => _graphicsPrefab;

    public int Atk => _atk;
    public int Def => _def;
    public int ActionPoints => _actionPoints;
    public int Mor => _mor;

    public int AuraAtk => _auraAtk;
    public int AuraDef => _auraDef;
    public int AuraMor => _auraMor;

    public ActionType AllowedActions => _allowedActions;
    public bool CanPerformAction(ActionType action) => action == ActionType.None || (_allowedActions & action) != 0;
}
