using UnityEngine;

public class UnitsSO : ScriptableObject
{
    [SerializeField] string _id;
    [SerializeField] string _displayName;

    [SerializeField] int _atk;
    [SerializeField] int _def;
    [SerializeField] int _mor;
    [SerializeField] int _actionPoints;

    [SerializeField] GameObject _graphicsPrefab;

    public int Atk => _atk;
    public int Def => _def;
    public int ActionPoints => _actionPoints;
    public int Mor => _mor;

    public GameObject GraphicsPrefab => _graphicsPrefab;

}
