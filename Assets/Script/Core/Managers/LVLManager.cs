using System.Collections.Generic;
using UnityEngine;

public class LVLManager : MonoBehaviour, IGameEventListener
{
    [Header("LVL Reference")]
    [SerializeField] private TurnManager _turnManager;
    [SerializeField] private HexGrid _map;
    [SerializeField] private UnitsRenderer _unitsRenderer;

    [Header("LVL Settings")]
    [SerializeField] private int _numbersOfTurns = 10;
    [SerializeField] private float _scoreToWin = 30;
    [SerializeField] private float _scoreForOccupation = 10;

    [Header("Events")]
    [SerializeField] private GameEventSO _winEvent;
    [SerializeField] private GameEventSO _loseEvent;
    [SerializeField] private GameEventSO _boardChangedEvent;

    private List<SpezzoneRuntime> _spezzoniOfLVL = new List<SpezzoneRuntime>();
    private List<PoliceRuntime> _policeOfLVL = new List<PoliceRuntime>();
    private List<HexCell> _objectiveCells = new List<HexCell>();

    private bool _gameOver = false;
    private float _currentScore;
    private int _currentTurn;

    public TurnManager TurnManager => _turnManager;
    public HexGrid Map => _map;
    public UnitsRenderer Renderer => _unitsRenderer;

    public List<SpezzoneRuntime> Spezzoni => _spezzoniOfLVL;
    public List<PoliceRuntime> Police => _policeOfLVL;

    public bool IsGameActive => !_gameOver;
    public int CurrentTurn => _currentTurn;
    public float CurrentScore => _currentScore;

    public int Cohesion { get; private set; }

    private void OnEnable()
    {
        _currentScore = 0;
        _currentTurn = _numbersOfTurns;
        _turnManager.EndPlayerTurnEvent.Subscribe(this);

        RefreshObjectiveCells();
    }

    private void Start()
    {

        UnitsSetup[] allSetups = FindObjectsByType<UnitsSetup>(FindObjectsInactive.Exclude);
        foreach (var setup in allSetups)
        {
            AbstractUnitsRunTime unit = setup.Initialize();
            if (unit == null) continue;
            if (unit is SpezzoneRuntime spezzone)
                _spezzoniOfLVL.Add(spezzone);
            else if (unit is PoliceRuntime police)
            {
                _policeOfLVL.Add(police);
            }

            _unitsRenderer.SpawnUnits(unit, setup.gameObject);

            // INIZIALIZZA UNITMOVEMENT
            GameObject unitGO = _unitsRenderer.GetGameObject(unit);
            if (unitGO != null)
            {
                unitGO.GetComponentInParent<SelectionOutline>()?.Initialize(unit);
                UnitMovement movement = unitGO.GetComponent<UnitMovement>();
                if (movement != null)
                    movement.Initialize(unit);
            }

        }

        RefreshBoardState();
    }


    private void OnDisable()
    {
        _turnManager.EndPlayerTurnEvent.Unsubscribe(this);
    }

    public void OnEventRaised()
    {
        _currentTurn--;

        foreach (var cell in _objectiveCells)
        {
            if (cell.OccupiedBy is SpezzoneRuntime)
            {
                _currentScore += _scoreForOccupation;
                Debug.Log($"guadagni {_scoreForOccupation}, punteggio: {_currentScore}");
            }
        }

        if (_currentTurn == 0)
        {
            Debug.Log("LVLOver");

            if (_currentScore >= _scoreToWin)
            {

                _winEvent.Raise();
                _gameOver = true;
                _turnManager.enabled = false;
                Debug.Log("You Win");
            }
            else
            {
                _loseEvent.Raise();
                _gameOver = true;
                Debug.Log("You Lost");
                _turnManager.enabled = false;
            }
        }
    }

    private void RefreshObjectiveCells()
    {
        _objectiveCells.Clear();
        foreach (var cell in _map.GetAllCells())
        {
            if (cell.Type != null && cell.Type.IsObjective)
                _objectiveCells.Add(cell);
        }
        Debug.Log($"Trovate {_objectiveCells.Count} celle obiettivo nella mappa.");
    }


    public void RestartLVL()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
        UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void RefreshBoardState()
    {
        ApplyAuras();
        RecalculateCohesion();
        _boardChangedEvent?.Raise();
    }

    private void ApplyAuras()
    {
        bool someoneFell;
        do
        {
            someoneFell = false;

            var pending = new List<(AbstractUnitsRunTime unit, int bonus)>();

            foreach (var unit in _spezzoniOfLVL)
                if (unit.IsAlive)
                    pending.Add((unit, TacticalQuery.GetAuraBonus(unit, _map).Mor));

            foreach (var police in _policeOfLVL)
                if (police.IsAlive)
                    pending.Add((police, TacticalQuery.GetAuraBonus(police, _map).Mor));

            foreach (var (unit, bonus) in pending)
            {
                unit.ApplyAuraMorale(bonus);
                if (!unit.IsAlive)
                {
                    someoneFell = true;
                    _unitsRenderer.UpdateView(unit);
                }
            }

        } while (someoneFell);
    }

    private void RecalculateCohesion()
    {
        int total = 0;
        foreach (var unit in _spezzoniOfLVL)
        {
            if (!unit.IsAlive) continue;
            foreach (HexCoordinates n in unit.PositionCell.Coordinates.GetNeighbors())
            {
                if (!_map.TryGetCell(n, out HexCell cell)) continue;
                if (cell.OccupiedBy is SpezzoneRuntime other && other.IsAlive)
                    total += 10;
            }
        }
        Cohesion = total;
    }

    public bool CheckCohesionDefeat()
    {
        if (_gameOver) return true;
        if (Cohesion > 0) return false;

        Debug.Log("Corteo disperso: coesione a zero");
        _loseEvent.Raise();
        _gameOver = true;
        _turnManager.enabled = false;
        return true;
    }
}
