using System.Collections.Generic;
using UnityEngine;

public class LVLManager : MonoBehaviour, IGameEventListener
{
    [Header("LVL Reference")]
    [SerializeField] private TurnManager _turnManager;
    [SerializeField] private HexGrid _map;
    [SerializeField] private UnitsRenderer _unitsRenderer;

    [Header("LVL Settings")]
    // L'obiettivo che il corteo ha dichiarato di voler prendere: è la condizione di
    // vittoria del livello (GDD 20.4). Oggi lo decide il livello; domani lo deciderà il
    // volantino scritto in Assemblea, e questo campo verrà scritto da fuori.
    [SerializeField] private ObjectiveSO _declaredObjective;

    [Header("Events")]
    [SerializeField] private GameEventSO _winEvent;
    [SerializeField] private GameEventSO _loseEvent;
    [SerializeField] private GameEventSO _boardChangedEvent;

    private List<SpezzoneRuntime> _spezzoniOfLVL = new List<SpezzoneRuntime>();
    private List<PoliceRuntime> _policeOfLVL = new List<PoliceRuntime>();

    private ObjectiveRuntime _declared;

    private bool _gameOver = false;
    private int _currentTurn;

    public TurnManager TurnManager => _turnManager;
    public HexGrid Map => _map;
    public UnitsRenderer Renderer => _unitsRenderer;

    public List<SpezzoneRuntime> Spezzoni => _spezzoniOfLVL;
    public List<PoliceRuntime> Police => _policeOfLVL;

    public bool IsGameActive => !_gameOver;

    /// <summary>Turni giocati finora. ⚠ Conta in SU: non c'è un limite di turni, e il
    /// contatore non fa perdere (GDD 20.4-bis, decisione parcheggiata).</summary>
    public int CurrentTurn => _currentTurn;

    public ObjectiveRuntime DeclaredObjective => _declared;
    public IReadOnlyList<ObjectiveRuntime> Objectives => _map != null ? _map.Objectives : null;

    public int Cohesion { get; private set; }

    private void OnEnable()
    {
        _currentTurn = 0;
        _turnManager.EndPlayerTurnEvent.Subscribe(this);
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

        ResolveDeclaredObjective();
        RefreshBoardState();
    }

    private void OnDisable()
    {
        _turnManager.EndPlayerTurnEvent.Unsubscribe(this);
    }

    /// <summary>
    /// Aggancia l'ObjectiveSO dichiarato al suo ObjectiveRuntime, che vive sulla griglia.
    /// </summary>
    private void ResolveDeclaredObjective()
    {
        _declared = null;

        if (_declaredObjective == null)
        {
            Debug.LogWarning("[LVL] No declared objective on this level: the level cannot be won");
            return;
        }

        foreach (ObjectiveRuntime objective in _map.Objectives)
        {
            if (objective.Data == _declaredObjective)
            {
                _declared = objective;
                Debug.Log($"[LVL] Declared objective: {_declared} ({_declared.Required} cell-turn(s) needed)");
                return;
            }
        }

        Debug.LogError($"[LVL] Declared objective '{_declaredObjective.name}' is not on this map: check the Objectives array on HexMapSO");
    }

    public void OnEventRaised()
    {
        if (_gameOver) return;

        _currentTurn++;

        // Un turno di occupazione per ogni obiettivo. L'accumulo si azzera da solo se in
        // questo turno non c'era nessuno sopra (vedi ObjectiveRuntime.Tick).
        foreach (ObjectiveRuntime objective in _map.Objectives)
        {
            bool claimedNow = objective.Tick();
            if (claimedNow && objective == _declared) WinLevel();
        }
    }

    private void WinLevel()
    {
        Debug.Log($"[LVL] Declared objective claimed on turn {_currentTurn}: you win");
        _winEvent.Raise();
        _gameOver = true;
        _turnManager.enabled = false;
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

        Debug.Log("[LVL] Corteo dispersed: cohesion at zero");
        _loseEvent.Raise();
        _gameOver = true;
        _turnManager.enabled = false;
        return true;
    }
}