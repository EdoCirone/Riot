using System.Collections.Generic;
using UnityEngine;

public class LVLManager : MonoBehaviour, IGameEventListener
{
    [Header("LVL Reference")]
    [SerializeField] private TurnManager _turnManager;
    [SerializeField] private HexGrid _map;
    [SerializeField] private List<HexCell> _objectiveCells = new List<HexCell>();
    
    [Header("LVL Settings")]
    [SerializeField] private int _numbersOfTurns = 10;
    [SerializeField] private float _scoreToWin = 500;

    [Header("Events")]
    [SerializeField] private GameEventSO _winEvent;
    [SerializeField] private GameEventSO _loseEvent;

    private List<SpezzoneRuntime> _spezzoniOfLVL = new List<SpezzoneRuntime>();
    private float _currentScore;
    private int _currentTurn;
    private HexMapSO _mapSO;

    public HexGrid Map => _map;
    public int CurrentTurn => _currentTurn;
    public float CurrentScore => _currentScore;

    private void OnEnable()
    {
        _mapSO = _map.HexMapData;
        _currentScore = 0;
        _currentTurn = _numbersOfTurns;
        _turnManager.EndTurnEvent.Subscribe(this);
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
                _currentScore += 1;
            }
        }

        if (_currentTurn == 0)
        {
            Debug.Log("LVLOver");

            if (_currentScore > _scoreToWin)
            {

                _winEvent.Raise();
                Debug.Log("You Win");
            }
            else
            {
                _loseEvent.Raise();
                Debug.Log("You Lost");
            }
        }
    }
}
