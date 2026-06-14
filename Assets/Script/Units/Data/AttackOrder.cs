using UnityEngine;

public class AttackOrder 
{
    private AbstractUnitsRunTime _atk;
    private AbstractUnitsRunTime _def;

    public AbstractUnitsRunTime Atk => _atk;
    public AbstractUnitsRunTime Def => _def;   

    public AttackOrder(AbstractUnitsRunTime atk, AbstractUnitsRunTime def)
    {
        _atk = atk;
        _def = def;
    }
}
