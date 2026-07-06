using UnityEngine;

public class BarricadeRuntime
{
    private BarricadeSO _barricadeSO;

    public BarricadeSO BarricadeSO => _barricadeSO;

    public BarricadeRuntime(BarricadeSO barricadeSO)
    {
        _barricadeSO = barricadeSO;
    }
}
