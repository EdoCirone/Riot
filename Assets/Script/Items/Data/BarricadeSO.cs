using UnityEngine;

[CreateAssetMenu(fileName = "BarricadeSO", menuName = "RIOT/Items/BarricadeSO")]
public class BarricadeSO : ItemSO
{

    public override ActionType Action => ActionType.Barricade;

}
