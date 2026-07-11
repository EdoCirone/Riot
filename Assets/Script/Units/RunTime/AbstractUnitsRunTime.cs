using UnityEngine;

public abstract class AbstractUnitsRunTime 
{
    protected HexCell _positionCell;
    protected UnitsStatus _status;
    protected int _morale;
    protected int _actionPoints;
    protected int _maxActionPoints;
    protected int _maxMorale;
    protected bool _isSeated;
    
    public int ActionPoints => _actionPoints;
    public int MaxActionPoints => _maxActionPoints;
    public int Morale => _morale;
    public int MaxMorale => _maxMorale;
    
    public bool IsSeated => _isSeated;
    
    public abstract int Atk { get; }
    public abstract int Def { get; }

    public HexCell PositionCell => _positionCell;
    public UnitsStatus Status => _status;

    public abstract GameObject GraphicsPrefab { get; }

    protected AbstractUnitsRunTime (HexCell positionCell, UnitsStatus status, int morale, int actionPoints)
    {
        _positionCell = positionCell;
        _status = status;
        _morale = morale;
        _maxMorale = morale;
        _actionPoints = actionPoints;
        _maxActionPoints = actionPoints;

    }
    #region PointActions
    public bool TrySpendActionPoint(int amount)
    {
        if (_actionPoints < amount) return false;
        _actionPoints -= amount;
        return true;
    }
    public void RefillActionPoints()
    {
        _actionPoints = _maxActionPoints;
    }

    #endregion

    public bool SetPosition(HexCell arriveCell)
    {
        bool isSucces = arriveCell.TryOccupy(this);
        if (isSucces)
        {
            _positionCell.Vacate();
            _positionCell = arriveCell;
        }
        return isSucces;
    }

    public void SitDown() => _isSeated = true;
    public void StandUp() => _isSeated = false;

    public void GainMorale(int amount)
    {
        _morale = Mathf.Min(_morale + amount, _maxMorale);
    }

    public void LoseMorale(int amount)
    {
        _morale = Mathf.Max(_morale - amount, 0);
        if (_morale == 0)
        {
            Disperse();
        }
    }

    public void Disperse()
    {
        _status = UnitsStatus.Disperse;
        _positionCell.Vacate();
    }
}
