using UnityEngine;

public abstract class AbstractUnitsRunTime
{
    protected HexCell _positionCell;
    protected UnitsStatus _status;

    protected int _morale;
    protected int _actionPoints;
    protected int _maxActionPoints;
    protected int _maxMorale;

    protected int _auraMoraleBonus;
    protected int _panicTurnsLeft;

    protected bool _isSeated;
    protected virtual bool CanBeArrested => false;

    public abstract string DisplayName { get; }

    public override string ToString() => DisplayName;

    public int ActionPoints => _actionPoints;
    public int MaxActionPoints => _maxActionPoints;
    public int Morale => _morale;
    public int BaseMorale => _morale - _auraMoraleBonus;
    public int MaxMorale => _maxMorale + _auraMoraleBonus;
    public bool IsSeated => _isSeated;
    public int PanicTurnsLeft => _panicTurnsLeft;
    public bool IsPanicked => _panicTurnsLeft > 0;

    public abstract Sprite Avatar { get; }
    public abstract int Atk { get; }
    public abstract int Def { get; }

    public abstract int AuraAtk { get; }
    public abstract int AuraDef { get; }
    public abstract int AuraMor { get; }

    public HexCell PositionCell => _positionCell;
    public UnitsStatus Status => _status;
    public bool IsAlive => _status == UnitsStatus.Alive;
    public void ClearPanic() => _panicTurnsLeft = 0;

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
        if (amount <= 0)
            return false;

        if (_actionPoints < amount)
            return false;

        _actionPoints -= amount;
        return true;
    }
    public void RefillActionPoints()
    {
        _actionPoints = _maxActionPoints;
    }

    #endregion

    public abstract bool CanPerformAction(ActionType action);
    public bool SetPosition(HexCell arriveCell)
    {
        if (arriveCell == null)
            return false;

        bool isSuccess = arriveCell.TryOccupy(this);

        if (isSuccess)
        {
            _positionCell.Vacate(this);
            _positionCell = arriveCell;
        }

        return isSuccess;
    }

    public void SitDown() => _isSeated = true;
    public void StandUp() => _isSeated = false;

    public void ApplyPanic(int turns)
    {
        if (turns <= 0) return;
        _panicTurnsLeft = Mathf.Max(_panicTurnsLeft, turns);
    }

    public void TickPanic()
    {
        if (_panicTurnsLeft > 0) _panicTurnsLeft--;
    }

    public void ApplyAuraMorale(int bonus)
    {
        int delta = bonus - _auraMoraleBonus;
        if (delta == 0) return;

        _auraMoraleBonus = bonus;
        _morale += delta;

        if (_morale > MaxMorale) _morale = MaxMorale;
        if (_morale <= 0) RemoveFromBoard(MoraleLossCause.AuraWithdrawn);
    }
    public void GainMorale(int amount)
    {
        if (amount <= 0)
            return;

        _morale = Mathf.Min(_morale + amount, MaxMorale);
    }

    public void LoseMorale(
        int amount,
        MoraleLossCause cause = MoraleLossCause.Other)
    {
        if (amount <= 0)
            return;

        _morale = Mathf.Max(_morale - amount, 0);

        if (_morale == 0)
            RemoveFromBoard(cause);
    }

    private void Disperse()
    {
        _status = UnitsStatus.Disperse;
        _positionCell.Vacate(this);
    }

    private void Arrest()
    {
        _status = UnitsStatus.Arrested;
        _positionCell.Vacate(this);
    }

    public void RemoveFromBoard(MoraleLossCause cause)
    {
        if (cause == MoraleLossCause.PoliceContact && CanBeArrested) Arrest();
        else Disperse();

        Debug.Log($"[REMOVED] {this} -> {_status} (cause: {cause})");
    }
}
