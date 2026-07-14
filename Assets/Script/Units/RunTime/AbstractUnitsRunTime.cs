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

    protected MoraleEventSO _moraleEvent;

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

    protected AbstractUnitsRunTime(
        HexCell positionCell,
        UnitsStatus status,
        int morale,
        int actionPoints,
        MoraleEventSO moraleEvent)
    {
        _positionCell = positionCell;
        _status = status;
        _morale = morale;
        _maxMorale = morale;
        _actionPoints = actionPoints;
        _maxActionPoints = actionPoints;
        _moraleEvent = moraleEvent; 
    }

    public void GainMorale(int amount)
    {
        int oldMorale = _morale;
        _morale = Mathf.Min(_morale + amount, _maxMorale);
        int actualGain = _morale - oldMorale;

        if (actualGain > 0 && _moraleEvent != null)
        {
            _moraleEvent.Raise(new MoraleChangeData(this, actualGain, true));
        }
    }

    public void LoseMorale(int amount)
    {
        int oldMorale = _morale;
        _morale = Mathf.Max(_morale - amount, 0);
        int actualLoss = oldMorale - _morale;

        if (actualLoss > 0 && _moraleEvent != null)
        {
            _moraleEvent.Raise(new MoraleChangeData(this, actualLoss, false));
        }

        if (_morale == 0)
        {
            Disperse();
        }
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


    public void Disperse()
    {
        _status = UnitsStatus.Disperse;
        _positionCell.Vacate();
    }
}
