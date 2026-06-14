using UnityEngine;

[CreateAssetMenu(fileName = "SpezzoneSO", menuName = "RIOT/Units/SpezzoneSO")]
public class SpezzoneSO : ScriptableObject
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
