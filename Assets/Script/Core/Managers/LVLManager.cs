
using System.Collections.Generic;
using UnityEngine;

public class LVLManager : MonoBehaviour
{
    [Header("LVL Reference")]
    [SerializeField] private TurnManager _turnManager;
    [SerializeField] private HexGrid _map;
    [SerializeField] private UnitsRenderer _unitsRenderer;

    [Header("Flyer selection")]
    [Tooltip("Selection confirmed by the Assembly. If missing or invalid, the level uses its fallback settings.")]
    [SerializeField] private FlyerSelectionSO _flyerSelection;

    [Header("LVL Settings fallback")]
    [Tooltip("L'obiettivo dichiarato dal corteo: da qui parte la vittoria. Oggi lo decide " +
             "il livello, domani lo deciderà l'Assemblea.")]
    [SerializeField] private ObjectiveSO _declaredObjective;

    [Tooltip("L'appuntamento dato dal volantino: da qui parte il corteo. Oggi lo decide " +
             "il livello, domani lo deciderà l'Assemblea.")]
    [SerializeField] private MeetingPointSO _meetingPoint;

    [Header("Level time")]
    [Tooltip("Fallback start time until the flyer provides it.")]
    [Range(0, 23)]
    [SerializeField] private int _defaultStartHour = 12;

    [Range(0, 59)]
    [SerializeField] private int _defaultStartMinute;

    [Tooltip("How many in-game minutes pass each turn.")]
    [Min(1)]
    [SerializeField] private int _minutesPerTurn = 15;

    [Header("Run state (temporary)")]
    [Tooltip("Temporary Repression value until RunManager provides it.")]
    [Range(TensionRules.MinValue, TensionRules.MaxValue)]
    [SerializeField] private int _startingRepression;
    [Tooltip("Shared balance values for tension-generating actions.")]
    [SerializeField] private TensionSettingsSO _tensionSettings;

    [System.Serializable]
    public struct RosterEntry
    {
        [Tooltip("Unit prefab to spawn.")]
        public GameObject prefab;

        [Tooltip("Gear carried into the level. Placeholder for the Assembly's outfitting step.")]
        public UnitsSetup.StartingItem[] equipment;
    }

    [Tooltip("The corteo taking the street. Fixed list for now, later provided by the " +
             "Assembly. Cannot exceed the meeting point capacity.")]
    [SerializeField] private RosterEntry[] _startingRoster;

    [Header("Events")]
    [SerializeField] private GameEventSO _winEvent;
    [SerializeField] private GameEventSO _loseEvent;
    [SerializeField] private GameEventSO _boardChangedEvent;
    [SerializeField] private GameEventSO _tensionChangedEvent;

    [Header("Police garrison")]

    [Tooltip("Share of the free garrison pulled onto the objective declared in the flyer. " +
            "0 = the flyer changes nothing, 1 = everyone converges on it.")]
    [Range(0f, 1f)]
    [SerializeField] private float _declaredReinforcement = 0.5f;

    [Tooltip("How far an incident wakes up nearby police.")]
    [SerializeField] private int _alarmRadius = 4;

    [Tooltip("How many turns a woken unit stays hostile before returning to its post.")]
    [SerializeField] private int _alarmDuration = 3;

    [Tooltip("Print a map coverage report at Start: garrison per objective, and how far the " +
         "corteo has to walk to reach each one.")]
    [SerializeField] private bool _logCoverageDiagnostics;

    private List<SpezzoneRuntime> _spezzoniOfLVL = new();
    private List<PoliceRuntime> _policeOfLVL = new();
    private readonly HashSet<ObjectiveRuntime> _objectivesThatRaisedTension = new();

    private LevelTension _tension;
    private ObjectiveRuntime _declared;
    private ObjectiveSO _resolvedDeclaredObjective;
    private MeetingPointSO _resolvedMeetingPoint;
    private int _resolvedStartMinutesFromMidnight;

    private bool _gameOver = false;
    private int _currentTurn;
    private bool _isConfigured;

    public TurnManager TurnManager => _turnManager;
    public HexGrid Map => _map;
    public UnitsRenderer Renderer => _unitsRenderer;
    public LevelTension Tension => _tension;
    public TensionSettingsSO TensionSettings => _tensionSettings;

    public int CurrentTension => _tension?.Current ?? TensionRules.MinValue;

    public EngagementRules AppliedEngagementRules => _tension?.AppliedRules ?? EngagementRules.Containment;

    public List<SpezzoneRuntime> Spezzoni => _spezzoniOfLVL;
    public List<PoliceRuntime> Police => _policeOfLVL;

    public bool IsConfigured => _isConfigured;
    public bool IsGameActive => _isConfigured && !_gameOver;

    /// <summary>
    /// Indice del round corrente. Il round 0 inizia all'orario iniziale del livello.
    /// </summary>
    public int CurrentTurn => _currentTurn;
    public int CurrentTimeMinutes => LevelTimeRules.CalculateCurrentMinutes(
     _resolvedStartMinutesFromMidnight,
     _currentTurn,
     _minutesPerTurn);

    public bool IsDeadlineRound =>
        _resolvedDeclaredObjective != null
        && LevelTimeRules.HasReachedDeadline(
            CurrentTimeMinutes,
            _resolvedDeclaredObjective.DeadlineMinutesFromMidnight);

    public ObjectiveRuntime DeclaredObjective => _declared;
    public ObjectiveSO DeclaredObjectiveData => _resolvedDeclaredObjective;

    public IReadOnlyList<ObjectiveRuntime> Objectives => _map != null ? _map.Objectives : null;

    public int Cohesion { get; private set; }

    private void ResolveLaunchConfiguration()
    {
        _resolvedDeclaredObjective = _declaredObjective;
        _resolvedMeetingPoint = _meetingPoint;
        _resolvedStartMinutesFromMidnight =
            (_defaultStartHour * 60) + _defaultStartMinute;

        if (_flyerSelection == null || !_flyerSelection.HasSelection)
            return;

        _resolvedDeclaredObjective = _flyerSelection.DeclaredObjective;
        _resolvedMeetingPoint = _flyerSelection.MeetingPoint;
        _resolvedStartMinutesFromMidnight =
            _flyerSelection.StartMinutesFromMidnight;
    }

    private void Awake()
    {
        ResolveLaunchConfiguration();
        _isConfigured = ValidateReferences();

        if (!_isConfigured)
        {
            _gameOver = true;
            enabled = false;
            return;
        }

        int initialTension = TensionRules.GetInitialTension(_startingRepression);

        _tension = new LevelTension(initialTension);
    }

    private bool ValidateReferences()
    {
        List<string> errors = new();

        if (_turnManager == null)
        {
            errors.Add("TurnManager non assegnato");
        }
        else
        {
            _turnManager.CollectConfigurationErrors(this, errors);
        }

        if (_map == null)
        {
            errors.Add("HexGrid not assigned");
        }
        else if (_map.HexMapData == null)
        {
            errors.Add("HexGrid has no HexMapSO");
        }
        else if (HasScenePolice()
                 && !_map.HexMapData.HasPoliceStation)
        {
            errors.Add(
                "Police units are present but the map has no police station"
            );
        }

        if (_tensionChangedEvent == null)
            errors.Add("TensionChangedEvent not assigned");

        if (_unitsRenderer == null)
            errors.Add("UnitsRenderer non assegnato");

        if (_resolvedDeclaredObjective == null)
            errors.Add("Obiettivo dichiarato non assegnato");

        if (_winEvent == null)
            errors.Add("WinEvent non assegnato");

        if (_loseEvent == null)
            errors.Add("LoseEvent non assegnato");

        if (_startingRoster != null && _startingRoster.Length > 0)
        {
            if (_resolvedMeetingPoint == null)
                errors.Add("Roster presente ma MeetingPoint non assegnato");

            for (int i = 0; i < _startingRoster.Length; i++)
            {
                if (_startingRoster[i].prefab == null)
                    errors.Add($"Prefab mancante nel roster, elemento {i}");
            }
        }

        if (_tensionSettings == null)
            errors.Add("TensionSettings not assigned");

        if (errors.Count == 0)
            return true;

        Debug.LogError(
            $"[LVL] Configurazione non valida. Avvio del livello bloccato:\n- " +
            string.Join("\n- ", errors),
            this);

        return false;
    }

    private void OnEnable()
    {
        if (!_isConfigured) return;

        _currentTurn = 0;
    }

    private void Start()
    {
        if (!_isConfigured) return;

        Debug.Log(
                $"[TENSION] Repression {_startingRepression} " +
                $"-> initial tension {_tension.Current} " +
                $"({_tension.AppliedRules})"
                   );

        SpawnSceneUnits();
        SpawnRoster();

        ResolveDeclaredObjective();

        PoliceGarrisonCoordinator.Assign(
            _policeOfLVL,
            _map.Objectives,
            _declared,
            _unitsRenderer,
            _tension.AppliedRules,
            _tensionSettings.GetLeashRadius(_tension.AppliedRules),
            _declaredReinforcement
        );

        RefreshBoardState();

        if (_logCoverageDiagnostics)
            LogCoverageDiagnostics();
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

        if (_resolvedMeetingPoint == null)
        {
            Debug.LogError("[LVL] Roster declared but no meeting point: the corteo has nowhere to gather");
            return;
        }

        MeetingPointRuntime meeting = null;
        foreach (MeetingPointRuntime candidate in _map.MeetingPoints)
            if (candidate.Data == _resolvedMeetingPoint) { meeting = candidate; break; }

        if (meeting == null)
        {
            Debug.LogError($"[LVL] Meeting point '{_resolvedMeetingPoint.name}' is not on this map: check the Meeting Points array on HexMapSO");
            return;
        }

        if (_startingRoster.Length > meeting.Capacity)
        {
            Debug.LogError($"[LVL] Roster of {_startingRoster.Length} does not fit in {meeting} (capacity {meeting.Capacity}): the extra units will not spawn");
        }

        int index = 0;
        int spawned = 0;

        foreach (RosterEntry entry in _startingRoster)
        {
            if (entry.prefab == null) continue;

            while (index < meeting.Cells.Count && !TacticalQuery.IsCellAvailable(meeting.Cells[index]))
                index++;

            if (index >= meeting.Cells.Count)
            {
                Debug.LogError($"[LVL] No free cell left in {meeting}: {entry.prefab.name} not spawned");
                break;
            }

            HexCell cell = meeting.Cells[index++];

            GameObject instance = Instantiate(entry.prefab, _map.GridToWorld(cell.Coordinates), Quaternion.identity);
            UnitsSetup setup = instance.GetComponentInChildren<UnitsSetup>();

            if (setup == null)
            {
                Debug.LogError($"[LVL] {entry.prefab.name} has no UnitsSetup: not a unit prefab");
                Destroy(instance);
                continue;
            }

            AbstractUnitsRunTime unit = setup.Initialize(_map, cell);

            if (unit == null)
            {
                Debug.LogError($"[LVL] {entry.prefab.name} failed to initialize at {cell.Coordinates}: instance discarded");
                Destroy(instance);
                continue;
            }

            // L'equipaggiamento arriva dal roster, non dal prefab: due Black Bloc dello
            // stesso prefab devono poter portare cose diverse. È il posto che domani
            // riempirà l'Assemblea.
            if (unit is SpezzoneRuntime spezzone && entry.equipment != null)
            {
                foreach (UnitsSetup.StartingItem gear in entry.equipment)
                {
                    if (gear.item == null || gear.quantity <= 0) continue;
                    spezzone.Inventory.AddItem(gear.item, gear.quantity);
                }
            }

            RegisterUnit(unit, setup.gameObject);
            spawned++;
        }

        Debug.Log($"[LVL] Corteo gathered at {meeting}: {spawned} unit(s) of {meeting.Capacity} place(s)");
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

    /// <summary>
    /// Aggancia l'ObjectiveSO dichiarato al suo ObjectiveRuntime, che vive sulla griglia.
    /// </summary>
    private void ResolveDeclaredObjective()
    {
        _declared = null;

        if (_resolvedDeclaredObjective == null)
        {
            Debug.LogWarning(
                "[LVL] No declared objective on this level: the level cannot be won");
            return;
        }

        foreach (ObjectiveRuntime objective in _map.Objectives)
        {
            if (objective.Data == _resolvedDeclaredObjective)
            {
                _declared = objective;
                Debug.Log(
                    $"[LVL] Declared objective: {_declared} " +
                    $"({_declared.Required} cell-turn(s) needed)");
                return;
            }
        }

        Debug.LogError(
            $"[LVL] Declared objective '{_resolvedDeclaredObjective.name}' " +
            "is not on this map: check the Objectives array on HexMapSO");
    }

    public void CompleteRound()
    {
        if (_gameOver) return;

        foreach (ObjectiveRuntime objective in _map.Objectives)
        {
            bool claimedNow = objective.Tick();

            if (claimedNow && objective == _declared)
            {
                WinLevel();
                return;
            }
        }

        // Il round della scadenza è giocabile: la conquista ha priorità
        // sulla sconfitta per tempo.
        if (_resolvedDeclaredObjective != null
            && LevelTimeRules.HasReachedDeadline(
                CurrentTimeMinutes,
                _resolvedDeclaredObjective.DeadlineMinutesFromMidnight))
        {
            LoseByDeadline();
            return;
        }

        _currentTurn++;
    }

    private void WinLevel()
    {
        Debug.Log(
            $"[LVL] Declared objective claimed at " +
            $"{CurrentTimeMinutes / 60:00}:{CurrentTimeMinutes % 60:00}: you win");

        _winEvent.Raise();
        _gameOver = true;
        _turnManager.enabled = false;
    }

    private void LoseByDeadline()
    {
        Debug.Log(
            $"[LVL] Objective deadline reached at " +
            $"{CurrentTimeMinutes / 60:00}:{CurrentTimeMinutes % 60:00}: you lose");

        _loseEvent.Raise();
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
        AuraService.AuraResult result = AuraService.Resolve(_spezzoniOfLVL, _policeOfLVL, _map);

        foreach (AbstractUnitsRunTime removed in result.RemovedUnits)
        {
            _unitsRenderer.UpdateView(removed);
        }
    }

    private void RecalculateCohesion()
    {
        Cohesion = CohesionService.Calculate(_spezzoniOfLVL, _map);
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
    public bool ChangeTension(int delta, string reason)
    {
        if (_tension == null || delta == 0)
            return false;

        int previous = _tension.Current;

        if (!_tension.Change(delta))
            return false;

        Debug.Log(
            $"[TENSION] {previous} -> {_tension.Current}: " +
            reason
        );

        _tensionChangedEvent.Raise();

        return true;
    }

    /// <summary>
    /// Sveglia il presidio attorno a un incidente. ⚠ È l'unico modo per staccare la polizia
    /// dal posto: senza questo il presidio è una statua e il corteo gli passa accanto.
    /// </summary>
    public void RaiseAlarmAround(HexCell origin, string reason)
    {
        if (origin == null) return;

        int woken = 0;
        foreach (PoliceRuntime police in _policeOfLVL)
        {
            if (!police.IsAlive) continue;
            if (police.PositionCell.Coordinates.Distance(origin.Coordinates) > _alarmRadius) continue;

            police.RaiseAlarm(_alarmDuration);
            woken++;
        }

        if (woken > 0)
            Debug.Log($"[ALARM] {reason}: {woken} unit(s) woken for {_alarmDuration} turn(s)");
    }

    /// <summary>
    /// Entering an unclaimed objective raises the local alarm.
    /// The first entry into each objective also raises Tension.
    /// </summary>
    public void CheckObjectiveIntrusion(AbstractUnitsRunTime unit, HexCell cell = null)
    {
        if (unit is not SpezzoneRuntime)
            return;

        cell ??= unit.PositionCell;

        if (cell == null || !cell.IsObjective)
            return;

        ObjectiveRuntime objective = cell.Objective;

        if (objective.IsClaimed)
            return;

        RaiseAlarmAround(cell, $"{unit} entered {objective}");

        if (!_objectivesThatRaisedTension.Add(objective))
            return;

        ChangeTension(_tensionSettings.ObjectiveEntry, $"first entry into {objective}");
    }
    [ContextMenu("Log coverage diagnostics")]
    public void LogCoverageDiagnostics()
    {
        string report = LevelCoverageDiagnostics.Build(
                         _map,
                         _resolvedMeetingPoint,
                         _declared,
                         _spezzoniOfLVL,
                         _policeOfLVL);

        if (!string.IsNullOrEmpty(report))
            Debug.Log(report);
    }

    private static bool HasScenePolice()
    {
        UnitsSetup[] setups =
            FindObjectsByType<UnitsSetup>(
                FindObjectsInactive.Exclude
            );

        foreach (UnitsSetup setup in setups)
        {
            if (setup != null && setup.IsPoliceSetup)
                return true;
        }

        return false;
    }
}
