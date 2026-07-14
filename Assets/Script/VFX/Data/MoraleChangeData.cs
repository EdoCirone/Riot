using UnityEngine;

[System.Serializable]
public struct MoraleChangeData
{
    public AbstractUnitsRunTime Unit;
    public int Amount;
    public bool IsGain; // true = gain, false = loss

    public MoraleChangeData(AbstractUnitsRunTime unit, int amount, bool isGain)
    {
        Unit = unit;
        Amount = amount;
        IsGain = isGain;
    }
}