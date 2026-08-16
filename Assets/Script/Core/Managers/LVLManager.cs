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
    [Tooltip("L'obiettivo dichiarato dal corteo: da qui parte la vittoria. Oggi lo decide " +
             "il livello, domani lo deciderà l'Assemblea.")]
    [SerializeField] private ObjectiveSO _declaredObjective;

    [Tooltip("L'appuntamento dato dal volantino: da qui parte il corteo. Oggi lo decide " +
             "il livello, domani lo deciderà l'Assemblea.")]
    [SerializeField] private MeetingPointSO _meetingPoint;

    [Tooltip("Il corteo che scende in piazza. Oggi è una lista fissa, domani arriva dalla " +
             "composizione. Non può superare la capienza del punto di ritrovo.")]
    [SerializeField] private GameObject[] _startingRoster;

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
        SpawnSceneUnits();
        SpawnRoster();

        ResolveDeclaredObjective();
        RefreshBoardState();
    }

    /// <summary>Unità piazzate a mano in scena: oggi la polizia. La cella la deducono
    /// da dove sono state trascinate nell'editor.</summary>
    private void SpawnSceneUnits()
    {
        UnitsSetup[] allSetups = FindObjectsByType<UnitsSetup>(FindObjectsInactive.Exclude);
        foreach (var setup in allSetups)
            RegisterUnit(setup.Initialize(_map), setup.gameObject);
    }

    /// <summary>
    /// Il corteo nasce sulle celle del punto di ritrovo, una per unità.
    /// ⚠ La capienza non è un parametro: è quante celle è grande la piazza.
    /// </summary>
    private void SpawnRoster()
    {
        if (_startingRoster == null || _startingRoster.Length == 0) return;

        if (_meetingPoint == null)
        {
            Debug.LogError("[LVL] Roster declared but no meeting point: the corteo has nowhere to gather");
            return;
        }

        MeetingPointRuntime meeting = null;
        foreach (MeetingPointRuntime candidate in _map.MeetingPoints)
            if (candidate.Data == _meetingPoint) { meeting = candidate; break; }

        if (meeting == null)
        {
            Debug.LogError($"[LVL] Meeting point '{_meetingPoint.name}' is not on this map: check the Meeting Points array on HexMapSO");
            return;
        }

        if (_startingRoster.Length > meeting.Capacity)
        {
            Debug.LogError($"[LVL] Roster of {_startingRoster.Length} does not fit in {meeting} (capacity {meeting.Capacity}): the extra units will not spawn");
        }

        int index = 0;
        int spawned = 0;

        foreach (GameObject prefab in _startingRoster)
        {
            if (prefab == null) continue;

            // Salta le celle occupate invece di consumarle: un posto bloccato non deve
            // costare un'unità del corteo.
            while (index < meeting.Cells.Count && !TacticalQuery.IsCellAvailable(meeting.Cells[index]))
                index++;

            if (index >= meeting.Cells.Count)
            {
                Debug.LogError($"[LVL] No free cell left in {meeting}: {prefab.name} not spawned");
                break;
            }

            HexCell cell = meeting.Cells[index++];

            GameObject instance = Instantiate(prefab, _map.GridToWorld(cell.Coordinates), Quaternion.identity);
            UnitsSetup setup = instance.GetComponentInChildren<UnitsSetup>();

            if (setup == null)
            {
                Debug.LogError($"[LVL] {prefab.name} has no UnitsSetup: not a unit prefab");
                Destroy(instance);
                continue;
            }

            RegisterUnit(setup.Initialize(_map, cell), setup.gameObject);
            spawned++;
        }

        Debug.Log($"[LVL] Corteo gathered at {meeting}: {spawned} unit(s) of {meeting.Capacity} place(s)");

        Debug.Log($"[LVL] Corteo gathered at {meeting}: {_spezzoniOfLVL.Count} unit(s) of {meeting.Capacity} place(s)");
    }

    /// <summary>Punto unico di registrazione: liste, view, e inizializzazione dei componenti.</summary>
    private void RegisterUnit(AbstractUnitsRunTime unit, GameObject setupObject)
    {
        if (unit == null) return;

        if (unit is SpezzoneRuntime spezzone) _spezzoniOfLVL.Add(spezzone);
        else if (unit is PoliceRuntime police) _policeOfLVL.Add(police);

        _unitsRenderer.SpawnUnits(unit, setupObject);

        GameObject unitGO = _unitsRenderer.GetGameObject(unit);
        if (unitGO == null) return;

        unitGO.GetComponentInParent<SelectionOutline>()?.Initialize(unit);
        unitGO.GetComponent<UnitMovement>()?.Initialize(unit);
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