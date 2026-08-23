
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

    [Header("Police garrison")]
    [Tooltip("Quanto un poliziotto può allontanarsi dall'obiettivo che difende. " +
             "Domani sarà funzione di Repressione e Tensione.")]
    [SerializeField] private int _leashRadius = 4;

    [Tooltip("Share of the free garrison pulled onto the objective declared in the flyer. " +
            "0 = the flyer changes nothing, 1 = everyone converges on it.")]
    [Range(0f, 1f)]
    [SerializeField] private float _declaredReinforcement = 0.5f;

    [Tooltip("How far an incident wakes up nearby police.")]
    [SerializeField] private int _alarmRadius = 4;

    [Tooltip("How many turns a woken unit stays hostile before returning to its post.")]
    [SerializeField] private int _alarmDuration = 3;

    [Tooltip("Condotta del presidio. Interruttore manuale finché non esiste la Tensione.")]
    [SerializeField] private EngagementRules _engagementRules = EngagementRules.Containment;

    [Tooltip("Print a map coverage report at Start: garrison per objective, and how far the " +
         "corteo has to walk to reach each one.")]
    [SerializeField] private bool _logCoverageDiagnostics;

    private List<SpezzoneRuntime> _spezzoniOfLVL = new List<SpezzoneRuntime>();
    private List<PoliceRuntime> _policeOfLVL = new List<PoliceRuntime>();

    private ObjectiveRuntime _declared;

    private bool _gameOver = false;
    private int _currentTurn;
    private bool _isConfigured;

    public TurnManager TurnManager => _turnManager;
    public HexGrid Map => _map;
    public UnitsRenderer Renderer => _unitsRenderer;
    public EngagementRules EngagementRules => _engagementRules;

    public List<SpezzoneRuntime> Spezzoni => _spezzoniOfLVL;
    public List<PoliceRuntime> Police => _policeOfLVL;

    public bool IsConfigured => _isConfigured;
    public bool IsGameActive => _isConfigured && !_gameOver;

    /// <summary>Turni giocati finora. ⚠ Conta in SU: non c'è un limite di turni, e il
    /// contatore non fa perdere (GDD 20.4-bis, decisione parcheggiata).</summary>
    public int CurrentTurn => _currentTurn;
    public int LeashRadius => _leashRadius;

    public ObjectiveRuntime DeclaredObjective => _declared;
    public IReadOnlyList<ObjectiveRuntime> Objectives => _map != null ? _map.Objectives : null;

    public int Cohesion { get; private set; }

    private void Awake()
    {
        _isConfigured = ValidateReferences();

        if (_isConfigured)
            return;

        _gameOver = true;
        enabled = false;
    }

    private bool ValidateReferences()
    {
        List<string> errors = new List<string>();

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
            errors.Add("HexGrid non assegnata");
        }
        else if (_map.HexMapData == null)
        {
            errors.Add("HexGrid senza HexMapSO");
        }

        if (_unitsRenderer == null)
            errors.Add("UnitsRenderer non assegnato");

        if (_declaredObjective == null)
            errors.Add("Obiettivo dichiarato non assegnato");

        if (_winEvent == null)
            errors.Add("WinEvent non assegnato");

        if (_loseEvent == null)
            errors.Add("LoseEvent non assegnato");

        if (_startingRoster != null && _startingRoster.Length > 0)
        {
            if (_meetingPoint == null)
                errors.Add("Roster presente ma MeetingPoint non assegnato");

            for (int i = 0; i < _startingRoster.Length; i++)
            {
                if (_startingRoster[i].prefab == null)
                    errors.Add($"Prefab mancante nel roster, elemento {i}");
            }
        }

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
        _turnManager.EndPlayerTurnEvent.Subscribe(this);
    }

    private void OnDisable()
    {
        if (!_isConfigured)
            return;

        if (_turnManager == null || _turnManager.EndPlayerTurnEvent == null)
            return;

        _turnManager.EndPlayerTurnEvent.Unsubscribe(this);
    }

    private void Start()
    {
        if (!_isConfigured) return;

        SpawnSceneUnits();
        SpawnRoster();

        ResolveDeclaredObjective();
        AssignGarrisons();

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
        AuraService.AuraResult result =
            AuraService.Resolve(
                _spezzoniOfLVL,
                _policeOfLVL,
                _map
            );

        foreach (AbstractUnitsRunTime removed
                 in result.RemovedUnits)
        {
            _unitsRenderer.UpdateView(removed);
        }
    }

    private void RecalculateCohesion()
    {
        Cohesion = CohesionService.Calculate(
            _spezzoniOfLVL,
            _map
        );
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

    /// <summary>
    /// Ogni poliziotto riceve un obiettivo da presidiare, in DUE passate.
    /// 1ª — quello dichiarato sul suo componente, oppure il più vicino a dove si trova.
    /// 2ª — il volantino è pubblico: una quota del presidio libero si sposta sull'obiettivo
    /// DICHIARATO dal giocatore. È questo che rende "dichiarare" una scelta con un costo.
    /// ⚠ Chi ha un obiettivo scritto a mano sull'UnitsSetup è immune: quello è un ordine del
    /// level designer, non un ripiego, e non va sovrascritto da una percentuale.
    /// </summary>
    private void AssignGarrisons()
    {
        // rules e radius si conservano perché servono anche alla 2ª passata
        List<(PoliceRuntime police, bool pinned, EngagementRules rules, int radius)> assigned = new();

        foreach (PoliceRuntime police in _policeOfLVL)
        {
            ObjectiveRuntime target = null;
            bool pinned = false;

            GameObject go = _unitsRenderer.GetGameObject(police);
            UnitsSetup setup = go != null ? go.GetComponent<UnitsSetup>() : null;

            if (setup != null && setup.GuardedObjective != null)
            {
                foreach (ObjectiveRuntime candidate in _map.Objectives)
                    if (candidate.Data == setup.GuardedObjective) { target = candidate; break; }

                if (target == null)
                    Debug.LogError($"[GARRISON] {police}: declared objective '{setup.GuardedObjective.name}' is not on this map");
                else
                    pinned = true;
            }

            if (target == null) target = NearestObjective(police.PositionCell.Coordinates);

            EngagementRules rules = (setup != null && setup.OverrideEngagement)
                ? setup.EngagementRules
                : _engagementRules;

            int radius = (setup != null && setup.LeashRadiusOverride >= 0)
                ? setup.LeashRadiusOverride
                : _leashRadius;

            police.AssignGuard(target, rules, radius);
            assigned.Add((police, pinned, rules, radius));

            Debug.Log(target != null
                ? $"[GARRISON] {police} guards {target} — {rules}, radius {radius}"
                : $"[GARRISON] {police} has no objective to guard: it will roam");
        }

        ReinforceDeclaredObjective(assigned);
    }

    /// <summary>
    /// Il volantino è pubblico: la polizia sa dove punta il corteo e ci concentra una quota
    /// del presidio. Vengono richiamati i più VICINI all'obiettivo dichiarato fra quelli che
    /// non lo stanno già presidiando — quindi dichiarare apre un buco proprio ACCANTO a ciò
    /// che hai dichiarato. È lì che nasce il diversivo, senza doverlo progettare a parte.
    /// </summary>
    private void ReinforceDeclaredObjective(
        List<(PoliceRuntime police, bool pinned, EngagementRules rules, int radius)> assigned)
    {
        if (_declared == null) return;
        if (_declaredReinforcement <= 0f) return;

        List<(PoliceRuntime police, EngagementRules rules, int radius, int distance)> candidates = new();

        foreach (var entry in assigned)
        {
            if (entry.pinned) continue;
            if (!entry.police.IsAlive) continue;
            if (entry.police.GuardedObjective == _declared) continue;

            candidates.Add((entry.police, entry.rules, entry.radius,
                DistanceToObjective(entry.police.PositionCell.Coordinates, _declared)));
        }

        if (candidates.Count == 0)
        {
            Debug.Log($"[GARRISON] flyer is public but nobody can answer it: " +
                      $"every free unit already guards {_declared}, the rest are pinned");
            return;
        }

        candidates.Sort((a, b) => a.distance.CompareTo(b.distance));

        // ⚠ CeilToInt e non RoundToInt: con pochi poliziotti l'arrotondamento (che in Unity
        // è bancario, 0.5 -> 0) annullerebbe il richiamo senza dirlo. Il volantino non deve
        // mai poter essere ignorato: se c'è un candidato, almeno uno risponde.
        int toMove = Mathf.CeilToInt(candidates.Count * _declaredReinforcement);
        toMove = Mathf.Min(toMove, candidates.Count);

        for (int i = 0; i < toMove; i++)
        {
            var c = candidates[i];
            ObjectiveRuntime left = c.police.GuardedObjective;
            c.police.AssignGuard(_declared, c.rules, c.radius);
            Debug.Log($"[GARRISON] {c.police} pulled from {left} to the declared {_declared} (distance {c.distance})");
        }

        Debug.Log($"[GARRISON] flyer is public: {toMove} of {candidates.Count} free unit(s) reinforce {_declared}");
    }

    private static int DistanceToObjective(HexCoordinates from, ObjectiveRuntime objective)
    {
        int best = int.MaxValue;
        if (objective == null) return best;

        foreach (HexCell cell in objective.Cells)
        {
            int d = from.Distance(cell.Coordinates);
            if (d < best) best = d;
        }

        return best;
    }

    private ObjectiveRuntime NearestObjective(HexCoordinates from)
    {
        ObjectiveRuntime nearest = null;
        int best = int.MaxValue;

        foreach (ObjectiveRuntime objective in _map.Objectives)
        {
            int d = DistanceToObjective(from, objective);
            if (d < best) { best = d; nearest = objective; }
        }

        return nearest;
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

    /// <summary>Entrare in un obiettivo non rivendicato fa scattare l'allarme (GDD 19.6).</summary>
    public void CheckObjectiveIntrusion(AbstractUnitsRunTime unit)
    {
        if (unit is not SpezzoneRuntime) return;

        HexCell cell = unit.PositionCell;
        if (cell == null || !cell.IsObjective) return;
        if (cell.Objective.IsClaimed) return;

        RaiseAlarmAround(cell, $"{unit} entered {cell.Objective}");
    }

    [ContextMenu("Log coverage diagnostics")]
    public void LogCoverageDiagnostics()
    {
        string report = LevelCoverageDiagnostics.Build(
            _map,
            _meetingPoint,
            _declared,
            _spezzoniOfLVL,
            _policeOfLVL
        );

        if (!string.IsNullOrEmpty(report))
            Debug.Log(report);
    }
}
