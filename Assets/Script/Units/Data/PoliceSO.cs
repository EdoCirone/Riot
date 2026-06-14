using UnityEngine;

[CreateAssetMenu(fileName = "PoliceSO", menuName = "RIOT/Units/PoliceSO")]
public class PoliceSO : ScriptableObject
{
    [SerializeField] string _id;
    [SerializeField] string _displayName;

    [SerializeField] int _atk;
    [SerializeField] int _def;
    [SerializeField] int _mov;

    [SerializeField] GameObject _graphicsPrefab;

    public int Atk => _atk;
    public int Def => _def;
    public int Mov => _mov;
    public GameObject GraphicsPrefab => _graphicsPrefab;

}
