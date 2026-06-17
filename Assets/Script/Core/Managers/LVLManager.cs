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


    private void OnEnable()
    {
        _currentScore = 0;
        _currentTurn = _numbersOfTurns;
        _turnManager.EndTurnEvent.Subscribe(this);

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
        }

    }


    private void OnDisable()
    {
        _turnManager.EndTurnEvent.Unsubscribe(this);
    }

    public void OnEventRaised()
    {
        _currentTurn--;
        foreach (var cell in _objectiveCells)
        {
            if (cell.OccupiedBy != null)
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
}
