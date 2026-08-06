# CLAUDE.md

## Progetto
DISSENSO (ex RIOT) — gioco tattico a turni 2D in Unity 6000.4.5f1 (URP).
Il giocatore comanda un corteo politico (spezzoni) su una griglia esagonale flat-top contro forze di polizia.
Lingua team: italiano. Commit e commenti in italiano. Nomi variabili/classi in inglese.

## Unity Version
6000.4.5f1 — non aggiornare mai senza istruzione esplicita.

## Scene (VERIFICATO 27/07/26 — "Main.unity" NON esiste, il documento sbagliava)
Build settings, in quest'ordine: Boot → MainMenu → LVLTest.
- Assets/Scenes/Boot.unity — bootscene (video introduttivo → MainMenu)
- Assets/Scenes/MainMenu.unity — menu principale
- Assets/Scenes/LVLTest.unity — scena di gioco (è quella che il documento chiamava
  erroneamente Main.unity)
- Assets/_Recovery/0.unity — file di recupero da un crash dell'Editor, fuori dalle
  build settings. Da cancellare dopo aver controllato che non contenga lavoro.

---

# PARTE 1 — STATO REALE (cosa il codice fa oggi)

Questa sezione descrive il comportamento implementato e verificato. È la fonte
autorevole per qualunque check di coerenza codice/documento.

## Modello di gioco
- **Esecuzione immediata**: il giocatore dà un ordine a uno spezzone, l'ordine
  si esegue subito (movimento o attacco), poi può dare l'ordine successivo.
  NON esiste fase decisionale differita né risoluzione simultanea di fine turno.
- Turno giocatore: ordini immediati via click finché ha punti azione / vuole.
- "Fine turno" (tasto) passa la mano alla polizia.
- Turno polizia: `PoliceAI.ExecutePoliceActions` — coroutine sequenziale, una
  unità alla volta, ognuna agisce finché ha PA.
- Ricarica PA in DUE momenti distinti (NON simultanea): i PA della polizia si
  ricaricano in `TurnManager.EndTurn` (prima che la polizia agisca); i PA degli
  spezzoni si ricaricano in `ExecutePoliceTurn` DOPO che la polizia ha agito,
  poi si rilancia l'evento di fine turno.

## Architettura — regole fondamentali
- ScriptableObject (suffisso SO) = dati statici (SpezzoneSO, PoliceSO, HexTypeSO,
  HexMapSO, MovementSettingsSO, e gli event channel GameEventSO/UnitEventSO).
- Classe Runtime (suffisso Runtime) = stato vivo della partita
  (SpezzoneRuntime, PoliceRuntime, derivano da AbstractUnitsRunTime).
- MonoBehaviour = oggetti in scena e orchestrazione (i Manager, HexGrid,
  UnitMovement, UnitsRenderer, InputHandler, PoliceAI).
- UI = solo visualizzazione e input, mai logica di gameplay.
- Comunicazione via event channel SO (pattern Ryan Hipple): i sistemi si
  sottoscrivono direttamente agli asset SO. Channel presenti: GameEventSO,
  UnitEventSO, ActionEventSO, ItemEventSO, GameObjectEventSO, StringEventSO,
  EventMusicSO, sopra la base EventChannelSO.
- "Zero singleton statici": regola RISPETTATA. (Il documento affermava il
  contrario fino al 03/08/26 — era falso, vedi bug noti.) Unica eccezione di
  fatto: `AudioManager` è `DontDestroyOnLoad`, quindi è unico per costruzione, ma
  non espone nessun accesso statico — chi gli serve lo trova con
  `FindAnyObjectByType`.
- **Confine elaboratore/esecutore** (chiave architetturale):
  - `TacticalQuery` (classe STATICA pura, senza stato) = ELABORATORE. Risponde a
    domande di legalità/raggiungibilità. Non muta nulla. La griglia è sempre
    passata come parametro, mai come campo.
  - `TurnManager` (MonoBehaviour) = ESECUTORE. Esegue azioni che mutano stato
    (movimento, scontro, carica, spinta, dispersione). Per le domande di legalità
    chiama `TacticalQuery`.
- Logica prima, animazione dopo: l'esecuzione risolve lo stato logico, poi
  l'animazione mostra uno stato già risolto (vincolo architetturale rigido).

## Manager
GameManager (reset/quit) / LVLManager (setup unità, score, win/lose, celle
obiettivo) / TurnManager (esecuzione azioni, ciclo turni) / CameraManager
(pan, drag mouse, zoom, follow selezione — DOTween + InputSystem) / AudioManager
(AudioMixer, musica, SFX, save/load volumi in PlayerPrefs) / BootManager
(bootscene). PoliceAI orchestra il turno polizia.
RunManager: VERIFICATO 27/07/26 — NON esiste, zero riferimenti nel codice.

## Audio (VERIFICATO 03/08/26 — sezione riscritta, la precedente era obsoleta)
File: `Assets/Script/Audio/RunTime/AudioManager.cs`, `SceneMusicHandler.cs`,
`Assets/Script/Audio/Data/SFXSO.cs`,
`Assets/Script/UI/GeneralPanels/OptionPanelView.cs`.

- `AudioManager` (DontDestroyOnLoad) tiene un `AudioMixer` con tre parametri
  esposti: `VolumeMaster`, `VolumeMusic`, `VolumeSFX`. Due AudioSource serializzati
  (`_musicSource`, `_sfxSource`), che devono essere **figli** del GameObject
  protetto: `DontDestroyOnLoad` protegge la gerarchia a partire dall'oggetto su cui
  è chiamato, non i fratelli.
- **Conversione volume**: i parametri di un AudioMixer sono in decibel.
  lineare→dB `Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f`; dB→lineare
  `Mathf.Pow(10f, db / 20f)`. Il `Mathf.Max` evita `-Infinity` a valore 0. Il
  fattore 20 non è opzionale: senza, uno slider 0→1 copre 1 dB invece di 20.
- **Persistenza — due meccanismi, entrambi necessari**:
  1. I setter `SetGeneralAudio` / `SetMusicVolume` / `SetSFXVolume` scrivono SIA
     sul mixer SIA in `PlayerPrefs.SetFloat`. Il salvataggio è agganciato al DATO
     (il valore cambia → è salvato), non a un evento di UI. È questo che rende il
     sistema robusto: nessuna strada d'uscita dal pannello — tasto, cambio scena,
     alt-F4 — può più perdere l'impostazione. `SaveAudioSettings()` fa solo
     `PlayerPrefs.Save()` (flush su disco).
  2. `SceneManager.sceneLoaded` → `LoadAudioSettings()`. **Serve davvero**: i
     parametri esposti di un AudioMixer si resettano al valore autorale (0 dB) a
     ogni cambio scena. Accertato coi log: valore corretto applicato in Awake,
     riletto `db=0,00` subito dopo la transizione.
- `SFXSO` = un `GameEventSO` (trigger) + array di `AudioClip` + range di pitch.
  `Play(AudioSource)` sorteggia pitch e clip e fa `PlayOneShot`. `PickClip` usa un
  `do...while` con sentinella `_lastIndex = -1` per non ripetere due volte di fila
  la stessa clip (con una sola clip esce prima del loop, che sarebbe infinito).
- `AudioManager` si iscrive agli SFX in `OnEnable` costruendo
  `List<(GameEventSO evt, System.Action handler)>`: serve a conservare il
  riferimento all'handler lambda per disiscriverlo simmetricamente in `OnDisable`.
  Non è un dizionario perché non serve mai cercare per chiave, solo iterare tutto
  in fase di pulizia.
- `EventMusicSO` è l'event channel che veicola un AudioClip da riprodurre in loop.
  `SceneMusicHandler` (MonoBehaviour da mettere in scena) lo alza in `Start`.
- `OptionPanelView` NON apre né chiude nulla: `Open()` legge i volumi correnti dal
  mixer e li mette negli slider (con `SetValueWithoutNotify`, per non far scattare
  gli handler durante l'inizializzazione), poi aggancia i listener; `Close()` li
  sgancia e fa il flush. Chi decide la visibilità del pannello è tutt'altro codice.
  `Close()` ha un null-check d'ingresso perché viene chiamato anche prima che
  `Open()` sia mai girato (es. da `InGamePanelManager.Start`).

## UI — pannelli (VERIFICATO 03/08/26)
Esistono DUE meccanismi di visibilità distinti, che non vanno mescolati:
- `MenuPanelView` (MainMenu): il pannello resta **sempre attivo** e scivola dentro
  e fuori schermo via DOTween (`Show()`/`Hide()`), regolando
  `CanvasGroup.interactable/blocksRaycasts`. `MenuPanelView.Awake` calcola la
  posizione nascosta: chi chiama `Hide()` all'avvio deve farlo da `Start()`, non da
  `Awake()`, altrimenti c'è una corsa fra i due Awake e la posizione non è pronta.
- `SetActive(true/false)` (InGamePanelManager, per Win/Lose/Menu).
Se un pannello ha `MenuPanelView`, NON va anche disattivato con `SetActive(false)`:
`Show()` non riattiva un GameObject spento, e il pannello sparirebbe per sempre.
- `MenuPanelView` espone `OnPanelShown`/`OnPanelHidden` (UnityEvent). Cablaggio
  **asimmetrico fra le due scene, ed è voluto**: nel MainMenu sono collegati a
  `OptionPanelView.Open/Close`, perché `MainMenuPanelManager` passa da un pannello
  all'altro senza un tasto "chiudi" dedicato e così la chiusura scatta da qualunque
  strada; nel LVL vanno lasciati **vuoti**, perché `InGamePanelManager` li chiama
  esplicitamente — se sono collegati, `Open()`/`Close()` girano due volte per click
  (`UnityEvent.AddListener` non deduplica, a differenza di `GameEventSO.Subscribe`
  che usa `Contains`).

## Rendering 2D — sorting layer e luci (VERIFICATO 03/08/26)
Ordine dei Sorting Layer, che è l'ordine di disegno (il primo è il più arretrato):
`Default → BackGround → Ground → Outline → Characters → Building → Objects`.

- **Esagoni** su `Ground` (prefab `HexBase`, condiviso da tutti e tre gli `HexTypeSO`).
- **Outline di selezione** su `Outline`, ordine 0 — assegnato da codice in
  `SelectionOutline.BuildOutlineRenderers`.
- **Unità** su `Characters` (tutti i prefab `*Graphics`).

Il layer ha la precedenza sull'ordine, quindi con questa disposizione la gerarchia
regge sempre e non serve calcolare ordini relativi. Prima erano tutti su `Default`
con esagoni a 0 e outline a 0: a parità di layer **e** ordine il pareggio si rompe
sulla distanza dalla camera, entrambi a z=0, quindi l'esito era arbitrario e
cambiava spostando le celle — l'outline appariva a chiazze.

⚠ **La `Global Light 2D` ha una lista `Target Sorting Layers`.** I materiali sono
`Sprite-Lit-Default`: uno sprite su un layer fuori da quella lista **non riceve luce
e diventa nero**. Ogni volta che si aggiunge un layer va aggiunto anche lì. È
l'impostazione più lontana dal sintomo che produce, e "gli sprite sono diventati
neri" non fa pensare a una luce.

⚠ **Import degli sprite per l'outline**: lo shader disegna il bordo campionando i
texel dentro il quad, quindi serve **Mesh Type: Full Rect** (con `Tight` la mesh è
ritagliata sulla sagoma e il bordo non ha dove essere disegnato) ed **Extrude Edges
basso, ~4**. `PoliceStandardUnit.png` aveva Extrude **26** e Sprite Mode `Multiple`:
il primo riempiva di colore ventisei pixel di margine, il secondo faceva campionare
pixel fuori dal rettangolo dello sprite. Risultato: un blocco pieno invece di un bordo.

## Naming Convention
- Classi: PascalCase. Campi privati serializzati: _camelCase.
- Proprietà pubbliche: PascalCase. Metodi: PascalCase, verbo chiaro.
- Metodi che possono fallire: prefisso Try, restituiscono bool.
- Eventi: prefisso On. Metodi che lanciano eventi: prefisso Raise.
- ScriptableObject: suffisso SO. Runtime: suffisso Runtime. UI: suffisso UI.

## Griglia
- Esagonale flat-top. Coordinate axial (q, r). 6 direzioni.
- `HexGrid` (MonoBehaviour) genera le celle da `HexMapSO`, le tiene in
  Dictionary<HexCoordinates, HexCell>.
- `HexCell` tiene tipo (HexTypeSO) e occupante (AbstractUnitsRunTime).
- Distanza esagonale via HexCoordinates.Distance.
- `PathFinder` = A* (NB: attualmente MonoBehaviour, vedi debito sotto).

## Unità (stat REALI — riverificato 27/07/26)
`AbstractUnitsRunTime` espone:
- **Atk**, **Def** (astratte, da SpezzoneSO/PoliceSO), **Morale**, **ActionPoints**
  (+ i rispettivi max).
- **IsSeated** (bool) con `SitDown()` / `StandUp()` — supporta l'azione SitStand.
- **Avatar** (Sprite astratta) e **GraphicsPrefab**, per la UI e la view.
- Morale modificabile in entrambe le direzioni: `GainMorale` / `LoseMorale`.
- Stato: Alive / Disperse (enum UnitsStatus).
- Morale a 0 → Disperse (l'unità vacate la cella e sparisce dalla view).
- SpezzoneRuntime e PoliceRuntime differiscono per la fonte SO di Atk/Def,
  l'avatar e il prefab grafico.

## Scontro (CombatResolver — REALE, aggiornato 03/08/26)
- **Deterministico, nessun dado.** Confronto secco fra **valori effettivi**:
  `CombatResolver.Resolve(atk, def, map)` confronta `GetEffectiveAtk` e
  `GetEffectiveDef`, cioè statistica base **più aura di adiacenza**.
- I due metodi `GetEffectiveAtk/Def` sono pubblici di proposito: li usa anche la UI.
  Se un giorno l'interfaccia calcolasse i valori per conto suo si riaprirebbe la
  divergenza fra ciò che si vede e ciò che accade, già combattuta con `GetAttackOption`.
- Nessun altro modificatore (no fasce di Coesione, no malus distacco, no Zona Rossa).

## Aure di adiacenza (VERIFICATO 03/08/26) — GDD cap. 17
Ogni unità **trasmette** un bonus alle unità adiacenti **della stessa parte**; non a
sé stessa. `UnitsSO` dichiara `_auraAtk`, `_auraDef`, `_auraMor`; il Runtime li espone
con tre proprietà astratte; `TacticalQuery.GetAuraBonus(unit, map)` somma i sei vicini
e restituisce la struct annidata `TacticalQuery.AuraBonus`.

- **Atk e Def**: si leggono al volo dentro `CombatResolver`. Nessuno stato da mantenere.
- **Morale: è un PRESTITO**, non un aumento del massimale. `ApplyAuraMorale(bonus)`
  sposta **corrente e massimo insieme** di `delta`; quando il donatore si allontana il
  prestito rientra e **può uccidere** un'unità già bassa. Non è sfruttabile: attaccare
  e staccare è a somma zero, perché il prestito viene restituito per intero.
  `BaseMorale` (= `_morale - _auraMoraleBonus`) serve alla UI per mostrare
  "tuo (+prestato)" nella stessa forma di Atk e Def.
- Valori attuali: Anarchici 2/0/0, Black Bloc 2/1/0, Operai 0/2/0, Studenti 1/1/0,
  Pacifisti 0/0/2, Police 0/0/0.
- **Principio di design**: nessuna aura deve essere il sovrainsieme di un'altra,
  altrimenti il gruppo dominato non ha più motivo di essere portato.

## Coesione (VERIFICATO 03/08/26) — GDD cap. 17
- `LVLManager.Cohesion` = 10 per ogni adiacenza fra spezzoni vivi (= legami × 20).
  Due unità adiacenti 20, tre in fila 40, tre a triangolo 60.
- **Non alimenta nessun modificatore**: serve solo alla sconfitta e allo schermo di
  fine livello. I modificatori passano dalle aure.
- **Sconfitta a Coesione 0, controllata SOLO a fine turno del giocatore**
  (`TurnManager.EndTurn` → `LVLManager.CheckCohesionDefeat`). Mai durante il turno:
  con due unità, muovere la prima romperebbe l'unico legame e si perderebbe a metà
  mossa. Regola che ne discende: la dispersione temporanea è consentita, quella
  permanente no.
- `RefreshBoardState()` è il punto unico di riallineamento: applica le aure,
  ricalcola la Coesione, alza `BoardChangedEvent`. Va chiamato **ovunque cambi
  posizione o stato** — la regola pratica è "dove chiami `UpdateView`, chiama anche
  `RefreshBoardState`".
- `ApplyAuras()` è a **due fasi** (prima calcola tutto in una lista, poi applica) e
  dentro un `do...while`. Le due fasi rendono l'esito indipendente dall'ordine della
  lista; il ciclo implementa il **crollo a catena**: se qualcuno cade la griglia è
  cambiata, quindi si rifà il giro. Termina sempre perché ogni ripetizione richiede
  almeno una morte.
- ⚠ `ApplyAuras` può uccidere: deve chiamare `_unitsRenderer.UpdateView(unit)` sui
  caduti, altrimenti restano sprite fantasma sulla griglia. Fino a oggi solo
  `TurnManager` poteva far morire qualcuno, e lì `UpdateView` c'era già.

## Disperso e Arrestato (VERIFICATO 03/08/26) — GDD cap. 18
- `UnitsStatus` = `Alive`, `Arrested`, `Disperse`.
- **La causa decide il destino**: `LoseMorale(amount, MoraleLossCause)` inoltra a
  `RemoveFromBoard(cause)`, che è **l'unico punto** dove si decide fra arresto e
  dispersione. `PoliceContact` + `CanBeArrested` → `Arrest()`, tutto il resto →
  `Disperse()`. `CanBeArrested` è `virtual false` sulla base, `true` su
  `SpezzoneRuntime`: un poliziotto non viene mai arrestato, si ritira.
- `TurnManager.CauseFrom(source)` traduce "chi ha colpito" in causa.
- **Regola di stile obbligatoria**: mai confrontare con un singolo stato "morto".
  Si usa `unit.IsAlive` (= `_status == Alive`). Elencare gli stati morti significa
  che ogni stato aggiunto in futuro passa per vivo — è esattamente il bug della
  polizia dispersa che continuava ad attaccare, moltiplicato per ogni nuovo stato.

## Azioni e loro effetti
- **Scontro (Skirmish)**: richiede distanza esattamente 1. Costa 1 PA. Non sposta
  nessuno, intacca solo il Morale. Win → difensore -1 Morale; Lose → attaccante
  -1; Par → entrambi -1.
- **Carica (Charge)**: richiede distanza esattamente 3 IN LINEA RETTA PURA
  (HexDirectionFinder), con le 2 celle intermedie libere. Costa 4 PA
  (`TacticalQuery.ChargeCost`). L'attaccante si sposta adiacente al difensore, poi
  si risolve la spinta a domino (sotto). Par → nessuno si muove.
  **`ExecuteCharge` è una `IEnumerator` e va aspettata** — vedi bug noti.
- **Muovi+attacca**: per police a distanza diversa da 1 e 3, lo spezzone si
  avvicina (FindBestAdjacentCell + A*) e poi fa scontro. Richiede PA per il
  percorso + 1. Sfocia SOLO in scontro, mai in carica.

## Spinta a domino (VERIFICATO 05/08/26) — sostituisce CalculatePushDestination
`TurnManager.TryBuildPushChain` + `ApplyPushChain` + `ResolvePushOrRemove`.
`CalculatePushDestination` e `FoundNearCellAvailable` sono stati **rimossi**: la spinta
laterale su cella comune non esiste più.

- Si cammina all'indietro sulla direzione della spinta (delta fra pusher e pushed, che
  sono sempre adiacenti) raccogliendo la catena. **Nessun tetto alla lunghezza**: ci si
  ferma alla prima cella libera.
- **Fanno muro e interrompono la catena**: bordo mappa, `!IsWalkable`, barricata,
  **cella obiettivo**, **unità avversaria**, **unità seduta**.
- Catena chiusa → si spostano tutte, **dall'ultima alla prima** (obbligatorio:
  `SetPosition` passa da `TryOccupy`, che fallisce su cella ancora occupata).
- Catena bloccata → **nessuno si muove** e chi ha perso lo scontro esce di scena via
  `RemoveFromBoard(CauseFrom(pusher))`, quindi arresto se l'ha spinto la polizia,
  dispersione altrimenti. **Esce il difensore, non l'ultimo della fila**: chi viene
  schiacciato contro la linea di polizia è chi viene preso.
- `Lose` è simmetrico: la catena si costruisce dietro l'attaccante, fra i suoi.
- Il limite è lo **spazio alle spalle**, non la lunghezza della fila. Un corteo stretto
  fra due poliziotti non ha uscite: è voluto.
- ⚠ **Obiettivo = muro** significa che un corteo schierato davanti a una cella obiettivo
  ha la fila che finisce contro di essa, quindi si fa arrestare. Regola tematica scelta
  il 05/08/26 ("il ministero non si prende per spinta"): da confermare in playtest.
- ⚠ La **cella obiettivo blocca anche se libera**. Ci si cammina sopra, non ci si viene
  spinti.
- ⚠ `ApplyPushChain` teletrasporta N unità: l'animazione della spinta non esiste.
  Quando si farà, il gancio va **dopo** `PushResolution` (vedi bug noti, PlayCharge).
- **Coro (Chant)**: costa 3 PA. +1 Morale a chi lo lancia e a ogni SpezzoneRuntime
  vivo nelle 6 celle adiacenti. Nessun effetto sulla polizia.
- **Sedersi/Alzarsi (SitStand)**: sedersi costa 1 PA, alzarsi 2. Da seduto
  `SpezzoneRuntime.Def` vale `Def + 5` (il bonus vive nell'override di Def, non
  in AbstractUnitsRunTime: PoliceRuntime non lo ha).
- **Lancio (Throw)** (letto riga per riga il 06/08/26): costo e danno vivono
  sull'asset — `item.ActionPointCost` e `item.MoralLost`. `ExecuteThrow` verifica
  possesso dell'oggetto e PA, consuma entrambi, alza `ThrowEvent` e toglie Morale al
  bersaglio. ⚠ **Non verifica affatto la gittata**: si fida di `HandleActionClick`.
  Il vincolo di distanza esattamente 2 con un vicino calpestabile
  (`HasThrowPath`) vive solo in `TacticalQuery.GetValidTargets`, che però assume un
  costo fisso di 2 PA. Vedi bug noti: le due decisioni vanno unificate.
- **Barricata** (letta riga per riga il 06/08/26): costo su `item.ActionPointCost`,
  bersaglio una delle 6 celle adiacenti, `IsCellAvailable`. ⚠ `GetValidTargets` **non
  controlla i PA**, quindi l'highlight compare anche a 0 PA. ⚠ E non guarda
  `cell.Type.IsObjective`: si può barricare un obiettivo.

## Highlight (OrderPreviewRenderer)
- Alla selezione di uno spezzone: una sola BFS via `TacticalQuery.GetReachable`
  produce `visited` (celle raggiungibili entro budget PA), passato sia a
  HighlightReachable (celle blu) sia a HighlightAttackable.
- Celle raggiungibili: blu. Scontro disponibile: rosso. Carica: giallo.
  Muovi+attacca: rosso (stesso dello scontro — vedi nota).

## Animazione (UnitMovement, DOTween + Lerp)
- Movimento: Lerp smoothstep cella per cella.
- Scontro: PlaySkirmish — windup + lancio + recoil, tutto DOTween.
- Carica: PlayCharge — windup DOTween + rincorsa Lerp.
- Flip sprite verso la direzione del bersaglio/destinazione.

## Bootscene (Boot.unity / BootManager)
Sequenza a coroutine unica in `BootManager.BootSequence`:
video intro → fade bianco → loading → fade a nero → attivazione MainMenu.
- Il VideoPlayer parte PRIMA del fade iniziale, così il bianco scopre un video
  già in movimento (`WaitUntil(frame >= 0)` attende il primo frame presentato).
- Fine video rilevata via evento `loopPointReached`, MAI leggendo `!isPlaying`
  (quel flag è false sia prima di partire sia dopo la fine: i due stati sono
  indistinguibili e la coroutine prosegue subito dopo Play). Ogni attesa ha un
  fail-safe a tempo: una bootscene non deve poter restare bloccata.
- `FadeCanvas` anima l'alpha del CanvasGroup; `FadeImageColor` anima il colore
  dell'Image tenendo l'alpha a 1. Quando lo schermo è GIÀ coperto e serve solo
  cambiare tinta si usa il secondo: animare l'alpha scoprirebbe ciò che sta sotto.
- La sfumata d'ingresso della scena successiva spetta a quella scena, non qui.

## V0.1 — stato: COMPLETO
- Loop end-to-end vincibile e perdibile (LVLManager: score per occupazione celle
  obiettivo, soglia di vittoria, conteggio turni).
- Movimento, scontro, carica, muovi+attacca funzionanti.
- AI polizia base (avvicinamento allo spezzone più vicino + attacco).

---

# PARTE 2 — DESIGN NON IMPLEMENTATO (NON usare come riferimento per il codice)

Idee di design presenti in documenti precedenti ma SENZA codice corrispondente.
Da NON trattare come comportamento esistente. Elencate per memoria progettuale.

- Fase decisionale + risolutiva con ordine di Reattività: ABBANDONATA.
  Il gioco usa esecuzione immediata. Il dead code relativo è già stato rimosso.
- ZOC (zona di controllo: chi entra in cella adiacente si ferma e attiva scontro):
  non implementata.
- Stat Reattività / Aggressività / Coesione: non esistono nel codice.
- Modificatori di scontro (fasce Coesione, malus distacco, Zona Rossa): non esistono.
- 6 gruppi politici tipizzati (Pacifisti, Operai, Studenti, Anarchici, Black Bloc,
  Movimento): non esistono come tipi; ci sono SpezzoneSO/PoliceSO generici.
- Casualità nello scontro (dado): non esiste; lo scontro è deterministico.

---

# PARTE 3 — V0.2 (IN CORSO, non "in progettazione")

ATTENZIONE: questa sezione descriveva V0.2 come da progettare. Il check del
27/07/26 ha trovato il codice già scritto. Stato reale:

- **Inventario — IMPLEMENTATO**. `Inventory` (classe C# pura) con `List<InventorySlot>`,
  metodi `HasItem` / `AddItem` / `ConsumeItem` e `Slots` in sola lettura.
  `InventorySlot` = coppia ItemSO + Quantity. UI: `InventoryView`, `InventorySlotUI`.
- **Lancio — IMPLEMENTATO**. `ThrowItemSO`, `TurnManager.ExecuteThrow`,
  `ThrowObjectVFX`, canale `ThrowEvent` (UnitEventSO).
- **Barricata — IMPLEMENTATA**. `BarricadeSO` + `BarricadeRuntime` +
  `TurnManager.ExecuteBarricade`.
- **Coro (Chant) — IMPLEMENTATO, non previsto in questo documento**.
  `TurnManager.ExecuteChant` + `OrderPreviewRenderer.HighlightChantArea`.
- **Sedersi/Alzarsi (SitStand) — IMPLEMENTATO, non previsto in questo documento**.
  `TurnManager.ExecuteSitStand` + `AbstractUnitsRunTime.IsSeated`.

NON verificato: se queste azioni siano complete, bilanciate o collegate alla UI in
ogni percorso. Il check ha accertato che il codice ESISTE, non che sia finito.
DA FARE: leggere questi metodi e portarne il comportamento reale in PARTE 1,
poi svuotare questa sezione.

---

# BUG NOTI / DEBITO TECNICO

## Trovati nel triple check del 06/08/26 (Claude + ChatGPT + DeepSeek)
Lista completa con dove/cosa/perché e ordine di lavoro in
`D:\UnityProject\GDDRIOT\FIXLIST_2026-08-06.md`. Qui solo quelli che cambiano il
modo di ragionare sul codice.

- **⚠ ATTIVO E GRAVE — il giocatore può agire durante il turno della polizia.**
  `TurnManager.IsPoliceTurn` esiste ed è pubblico, ma **`InputHandler` non lo guarda
  mai**: `OnLeftClick` controlla solo `_isExecutingAction` e `IsGameActive`, le nove
  hotkey e i bottoni azione solo `_isExecutingAction`. Premuto Fine turno si può
  quindi selezionare uno spezzone e muoverlo mentre `PoliceAI` sta iterando e mutando
  la stessa griglia. Conseguenze: PA spesi nel turno avversario, percorsi calcolati su
  uno stato che cambia sotto, `SetPosition` che fallisce a metà catena.
  Fix: un predicato unico `CanAcceptPlayerInput` che includa `!_turnManager.IsPoliceTurn`,
  in cima a tutti e nove i punti d'ingresso.
  ⚠ **È questo bug che rende raggiungibile quello sotto**: due difetti innocui separati
  che, composti, corrompono la griglia. Regola generale che ne discende: **un bug di
  sincronizzazione non è mai "solo" di sincronizzazione** — apre la porta a tutti i
  controlli che qualcun altro ha dato per garantiti a monte.

- **⚠ ATTIVO in combinazione — un'unità può finire SOPRA una barricata.**
  `HexCell.TryOccupy` controlla solo `_occupiedBy == null`, mai `_barricade != null`.
  Tutti i percorsi normali filtrano a monte (`IsCellAvailable`, `HasChargeRoom`,
  `TryBuildPushChain` controllano la barricata), quindi da solo non è sfruttabile. Ma
  `UnitMovement.MoveCoroutine` **non rivalida la cella all'ingresso** e `ExecuteMovement`
  accetta una `List<HexCell>` calcolata prima: se il giocatore piazza una barricata su
  una cella del percorso mentre la polizia lo sta già percorrendo (possibile per il bug
  sopra), la polizia ci sale sopra.
  Fix a due livelli: `if (_barricade != null) return false;` in `TryOccupy`, e in
  `MoveCoroutine` **usare il valore di ritorno di `SetPosition`** (oggi buttato) per
  interrompere il movimento. Il secondo è quello che conta: la forma generale del
  problema è "percorso calcolato prima, applicato dopo, mai rivalidato", e tornerà col
  panico, che sposta più unità insieme.

- **⚠ ATTIVO — `EndTurn` prosegue dopo il game over.** `_endPlayerTurnEvent.Raise()` è
  **sincrono**: dentro, `LVLManager.OnEventRaised` può decretare fine partita, alzare
  win/lose e fare `_turnManager.enabled = false`. Al ritorno del listener, `EndTurn`
  **continua**: ricarica i PA della polizia e chiama `StartCoroutine(ExecutePoliceTurn())`,
  che a fine coroutine rialza `_startPlayerTurnEvent` su una partita conclusa.
  `enabled = false` non interrompe un metodo già in esecuzione — questo è certo e vale
  come regola generale. Fix: `if (!_lvlManager.IsGameActive) { _waitingForPolice = false; return; }`
  subito dopo il `Raise`, e la stessa guardia in `ExecutePoliceTurn`.

- **⚠ ATTIVO — divergenza highlight/esecuzione su SitStand, Throw e Barricade.**
  La chiusura fatta a luglio con `GetAttackOption` valeva solo per l'attacco; le altre
  azioni non hanno mai avuto lo stesso trattamento e `GetValidTargets` decide con numeri
  fissi che l'esecutore non usa:
  - `SitStand`: la query chiede `budget < 1`, ma **rialzarsi costa 2**. Unità seduta con
    1 PA → cella colorata, clic accettato, esecuzione rifiutata.
  - `Throw`: query `budget < 2` fisso, esecuzione `item.ActionPointCost`.
  - `Barricade`: la query **non controlla i PA affatto**.
  - `ExecuteThrow` per giunta **non verifica la gittata**: si fida di `HandleActionClick`.
  Fix: query che ricevano l'**unità** (e l'oggetto), non solo coordinata e budget —
  `GetSitStandCost(unit)`, `CanThrow(unit, target, item, map)`,
  `CanPlaceBarricade(unit, cell, item)` — e che gli esecutori chiamino le stesse.
  È un cambio di firma: farlo in una volta sola per tutte e tre.

- **`SelectionOutline` si iscrive a quattro eventi senza guardie** (né `_isValid` né
  null-check). È l'unico posto del progetto senza rete, e sta sui **prefab delle unità**:
  un campo non assegnato si moltiplica per ogni unità che spawna, e l'eccezione arriva
  dentro il `Start` di `LVLManager`, mentre sta costruendo il livello.
  (`CameraManager` non usa `_isValid` ma protegge ogni `Subscribe` con `if (event != null)`:
  quello va bene.)

- **`UnitsSetup.Initialize`: la guardia sta DOPO l'uso.** Nel `foreach` sull'inventario
  iniziale, `AddItem(s.item, s.quantity)` viene chiamato **prima** del `if (s.item == null
  || ...) continue`, che quindi non salta più niente. Una riga vuota nell'array inserisce
  uno slot con `Item = null`, che poi `InventorySlotUI.SetItem` dereferenzia.

- **Nessun `WaitUntil` ha un fail-safe.** `ExecuteCharge`, `ExecuteSkirmish` e
  `PoliceAI` aspettano un flag alzato da una callback di animazione. Se la callback non
  arriva (tween ucciso, GameObject disattivato), lato giocatore `_isExecutingAction`
  resta `true` e input e Fine turno si bloccano per sempre; lato IA `_waitingForPolice`
  resta `true` e il turno non torna mai. **Nessun errore, nessun log: il gioco si pianta.**
  In `BootManager` ogni attesa ha un timeout proprio per questo; qui no.

- **I costruttori Runtime ignorano `TryOccupy`, e `Vacate()` non controlla chi libera.**
  Due `UnitsSetup` sulla stessa coordinata → la seconda unità esiste in lista e in scena
  ma la cella indica la prima; quando la seconda si muove, il suo `Vacate()` **cancella
  dalla griglia la prima**. Fix: `UnitsSetup.Initialize` deve restituire `null` con un
  `LogError` se l'occupazione fallisce, e `Vacate(unit)` deve liberare solo se
  `_occupiedBy == unit`.

- **`LVLManager.OnEnable` legge la griglia prima che `HexGrid.Awake` l'abbia generata.**
  Unity garantisce `Awake` prima di `OnEnable` **sullo stesso componente**, non l'ordine
  incrociato fra GameObject. Se perde la corsa, `_objectiveCells` resta vuota: il punteggio
  non sale mai e si perde ogni livello per scadenza turni. Il log
  `Trovate N celle obiettivo nella mappa.` lo dice — se N è 0, è questo.
  Fix: spostare `RefreshObjectiveCells()` in `Start`.

- **La conversione coordinate è sparsa e non uniforme.** `UnitsRenderer.UpdateView` usa
  `Coordinates.ToWorldPosition(cellSize)` **senza** `_grid.transform.position`; `UnitsSetup`
  e `InputHandler` passano posizioni mondo a `FromWorldPosition` senza sottrarre l'offset;
  `TurnManager`, `UnitMovement`, `ThrowObjectVFX` e `HexGridRenderer` invece lo sommano.
  Oggi invisibile perché `MapManager` è a `(0,0,0)` (verificato in scena). Il giorno che si
  trasla la griglia si rompono tre cose diverse — clic, spawn e `UpdateView` — e sembreranno
  causate dallo spostamento. Fix: `GridToWorld`/`WorldToGrid` su `HexGrid`, e nessun altro
  script che somma `transform.position` a mano.

- **La regola `IsAlive` è già violata in quattro punti**: `TacticalQuery.GetAuraBonus`,
  `TurnManager.ExecuteChant`, `OrderPreviewRenderer.OnActionSelected` e `HighlightChantArea`
  confrontano con `UnitsStatus.Alive`. Con gli stati attuali il comportamento è identico,
  ma **il panico è il prossimo stato non-vivo in arrivo**: sostituzione meccanica, da fare
  ora che è gratis.

- **`Inventory.ConsumeItem` rimuove mentre itera.** `_slots.Remove(slot)` dentro un
  `foreach` su `_slots`. **Non lancia oggi**, e vale la pena sapere perché: l'eccezione
  arriva alla successiva `MoveNext()`, e il `return true;` sulla riga dopo esce prima che
  ci sia una successiva iterazione. Un `break` sarebbe altrettanto sicuro; un `continue`,
  o togliere il `return`, la fa esplodere. Da convertire in un `for` a indice decrescente.

- **`PlaySkirmish` e `PlayHitReaction` non impostano mai `_isMoving`.** Quindi `_isMoving`
  non significa "questa unità è occupata da un'animazione", significa "sta eseguendo
  `MoveCoroutine` o `ChargeSequence`". La guardia in cima a `PlaySkirmish` non protegge da
  due scontri sovrapposti né impedisce di avviare un movimento durante un tween di scontro.
  Oggi copre `_isExecutingAction` in `InputHandler`: cioè la protezione dipende da chi
  chiama, non dall'oggetto animato.

- **`PathFinder`: `if (minFCell != null)` su uno struct.** `HexCoordinates` è uno `struct`
  con `operator !=` sovraccaricato: compila (l'operatore viene sollevato alla forma nullable)
  ed è **sempre vero**. In più `FoundMinimumF` non ha un percorso che rappresenti "nessun
  risultato" — parte da `foundcells[0]` ed è chiamato solo dentro `while (foundCell.Count > 0)`.
  Dead code, il blocco si appiattisce senza cambiare niente.

- **`BootManager`: la PRIMA attesa non ha fail-safe.** Il `WaitUntil(frame >= 0)` che attende
  il primo frame presentato precede la costruzione del timer di sicurezza. Se la clip manca o
  non produce un frame, la bootscene resta sul nero e non arriva mai al timeout su
  `clipLength + 2`. Il commento "una bootscene non deve poter restare bloccata" quindi non è
  ancora vero per quel punto.

- **DECLASSATO — `ExecuteSitStand` / `ExecuteBarricade` senza `RefreshBoardState`.** Il primo
  check del 06/08/26 lo dava per bug attivo: **sbagliato**. `HandleActionClick` finisce con
  `SetSelectedAction(None)` e `OnActionComplete()`, che rialza `_unitSelectedEvent`, a cui
  `UnitStatsPanelView` è iscritto e su cui fa `Refresh()`. E né sedersi né barricare cambiano
  le adiacenze, quindi aura e coesione restano identiche. Resta **un'omissione architetturale**:
  manca solo `_boardChangedEvent`, che oggi ha un unico ascoltatore su un valore che qui non
  cambia. Diventerà un bug quando un altro sistema userà `BoardChanged` per invalidare percorsi
  o overlay. *Nota di metodo: la regola "dove chiami `UpdateView` chiama `RefreshBoardState`"
  resta valida, ma non basta a classificare la gravità — va guardato chi ascolta davvero.*

## Precedenti

- **Muovi+attacca combinato subottimale**: il comando combinato può rifiutare per
  costo-path quando FindBestAdjacentCell sceglie una cella adiacente per distanza
  diretta e non per costo di percorso reale (l'A* gira intorno agli ostacoli).
  Workaround: scomporre a mano (avvicinati, poi attacca). Da fixare facendo
  ordinare le adiacenti per costo-path, ma tocca anche l'highlight.
- **RISOLTO — divergenza highlight/esecuzione**: la query d'attacco unificata è
  stata fatta. `TacticalQuery.GetAttackOption` (+ struct `AttackOption`) è ora
  chiamata SIA da `OrderPreviewRenderer.HighlightAttackable` SIA da
  `InputHandler.ConfirmAttack`. Highlight ed esecuzione decidono con la stessa
  logica: la divergenza è chiusa per costruzione. Verificato 27/07/26.
- **Migrazione TacticalQuery — stato reale al 27/07/26**: in TacticalQuery ci sono
  GetReachable, GetValidTargets, IsCellAvailable, GetAttackOption, HasChargeRoom.
  `FindBestAdjacentCell` è rimasta in TurnManager (usata da PoliceAI) — il
  documento diceva erroneamente che era stata spostata.
- ~~**TurnManager.CanCharge è DEAD CODE, sicura da cancellare**~~ — **AFFERMAZIONE
  FALSA E PERICOLOSA, corretta il 06/08/26.** Era vera al 27/07/26; dal fix della
  carica asincrona (05/08/26) `CanCharge` è il **predicato che `PoliceAI` interroga
  prima di impegnare il turno** (`else if (distance == 3 && _turnManager.CanCharge(...))`).
  Cancellarla riaprirebbe il blocco del turno polizia a distanza 3 non allineata.
  *Nota di metodo: la voce è sopravvissuta perché descriveva uno stato vero al momento
  della scrittura e nessuno l'ha riverificata dopo il refactor. Una voce "sicura da
  cancellare" va riverificata con un grep PRIMA di agire, sempre.*
- ~~**OrderPreviewRenderer._turnManager è un campo inutile**~~ — **GIÀ RIMOSSO**,
  zero occorrenze nel file (verificato 06/08/26).
- ~~GameManager.instance è un singleton statico pubblico~~ — **AFFERMAZIONE FALSA,
  corretta il 03/08/26**. `GameManager.cs` non ha nessun campo statico: contiene
  solo `ResetLevel`, `PlayNewRun`, `BackToMain`, `OnApplicationQuit`. Grep di
  `GameManager.instance` e `static GameManager` su tutto `Assets/Script`: zero
  risultati. La regola "zero singleton statici" NON è violata da nessuna parte.
  Rimossi anche i campi statici morti di `AudioManager`.
  *Nota di metodo: questa voce è sopravvissuta a due check perché veniva riletta
  invece che riverificata. Vale la stessa regola del Documento di Progetto: per
  nomi, numeri e binding il documento riporta ciò che si è LETTO, e dice dove.*
- ~~**`GameManager.OnApplicationQuit()` ha un nome riservato Unity**~~ — **RISOLTO**,
  il metodo si chiama `QuitGame()` (verificato 06/08/26). La spiegazione resta qui
  perché la lezione vale per qualunque metodo con nome-messaggio Unity:
  ~~(aperto, verificato 03/08/26). Attenzione: **non è un bug attivo, è un rischio latente** —
  una prima stesura di questa voce lo dava per più grave di quanto sia.
  Il comportamento voluto (premendo X l'Editor esce dal Play) è corretto e va
  MANTENUTO. Il problema è solo il nome: Unity chiama da sé qualunque metodo
  chiamato `OnApplicationQuit` su ogni MonoBehaviour alla chiusura. Quindi premendo
  il bottone il corpo gira due volte (una dal bottone, una dal messaggio Unity
  scatenato dall'uscita dal Play). Oggi è innocuo — `PlayerPrefs.Save()` due volte
  non fa nulla e `Application.Quit()` in Editor è un no-op — ma il giorno che
  dentro finisce un salvataggio di run o un evento di analytics, quello parte due
  volte e la causa sarà invisibile. Fix: rinominare in `QuitGame()`, separare i
  rami con `#if UNITY_EDITOR / #else`, e riagganciare il bottone nell'Inspector
  (Unity non aggiorna da solo il collegamento quando rinomini).
- ~~AudioManager: conversione volume sbagliata~~ — **RISOLTO 31/07/26**: i tre
  setter usano `Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f`.
- ~~**AudioManager.Awake: guardia con `&&` invece di `||`**~~ — **RISOLTO**, la riga
  dice `if (_musicSource == null || _sfxSource == null)` (verificato 06/08/26).
  ⚠ **La seconda metà della voce è ancora aperta**: se la guardia scatta, `Awake`
  esce PRIMA di `DontDestroyOnLoad`, ma `OnEnable` si iscrive lo stesso agli eventi —
  l'AudioManager muore al cambio scena lasciando iscrizioni pendenti. Serve il
  pattern `_isValid` già usato da `InGamePanelManager` e `CohesionHUDView`.
- **I sei canali evento di combattimento sono SCOLLEGATI in scena** (verificato nel
  YAML di `LVLTest.unity` il 06/08/26). `TurnManager` dichiara `_skirmishWin/Lose/Par`
  e `_chargeWin/Lose/Par`: **tutti e sei valgono `fileID: 0`**. Il codice li alza con
  `?.Raise()`, quindi non crasha e non logga — fallimento silenzioso. A disco esistono
  solo i tre dello scontro (`WinCombactEvent`, `LoseCombactEvent`, `ParCombactEvent`,
  ancora orfani); i tre della carica **non esistono affatto** e vanno creati.
  Finché sono vuoti nessun SFX di combattimento è agganciabile: è il prerequisito di
  tutto il lavoro audio in coda.
  *(Questa voce sostituisce la vecchia "Asset evento orfani" del 31/07/26: i campi nel
  codice adesso ci sono, manca solo il cablaggio.)*
- ~~**SFXSO: `using UnityEngine.LightTransport;` è un import spurio**~~ — **GIÀ TOLTO**,
  zero occorrenze (verificato 06/08/26). Idem i `Debug.Log [AUDIO]` diagnostici.
- **SFXSO._lastIndex è stato mutabile su uno ScriptableObject**: gli SO sono asset
  condivisi e in Editor il valore sopravvive fra una sessione di Play e l'altra.
  Qui è innocuo (serve solo a non ripetere una clip), ma è un'eccezione alla regola
  "SO = dati statici", da tenere d'occhio se l'asset venisse usato da più
  AudioSource in parallelo.
- ~~**PoliceAI: un poliziotto disperso continuava ad agire**~~ — **RISOLTO 03/08/26.**
  `ExecutePoliceActions` controllava `Status == Disperse` solo all'inizio del turno di
  ogni unità, non dentro il `while (actedThisTurn && police.ActionPoints > 0)`. Un
  poliziotto che perdeva uno scontro e si disperdeva a metà del proprio turno spariva
  dalla vista ma continuava ad attaccare finché aveva PA. Fix: aggiunta la condizione
  `&& police.Status == UnitsStatus.Alive` al `while`.
  ⚠ **Causa profonda ancora presente**: `Disperse()` fa `_positionCell.Vacate()` ma
  lascia `_positionCell` che punta alla cella ormai vuota. Tutto il codice a valle
  continua a girare senza eccezioni calcolando distanze da una posizione che non
  esiste più — per questo il bug non produceva nessun errore. Ogni ciclo che itera
  unità deve verificare `Status` esplicitamente, non fidarsi della posizione.
- **⚠ Gli enum serializzati sono NUMERI, non nomi** (lezione costosa del 03/08/26).
  Passando `ActionType` da sequenziale a `[System.Flags]` con potenze di due, tutti
  i valori già salvati in scene/prefab/asset sono rimasti identici sul disco e hanno
  **cambiato significato**. I cinque `ActionSlotUI` del pannello azioni in
  `LVLTest.unity` avevano `_action` = 4, 1, 3, 5, 2 (numerazione vecchia:
  Chant, Charge, Barricade, SitStand, Throw). Con la nuova numerazione quegli stessi
  numeri valgono Barricade, Charge, **Charge+Throw**, **Charge+Barricade**, Throw:
  tre bottoni su cinque mandavano richieste combinate che nessuno aveva mai chiesto.
  **Fallimento silenzioso**: nessun errore, tutto compila, e il caso peggiore
  (`4 → Barricade`) sembra perfino un valore legittimo. Diagnosticato solo mettendo
  un log in `SetSelectedAction` che stampava l'azione richiesta: la riga
  "chiede Charge, Throw" da un click su un bottone singolo ha chiuso il caso.
  Da tastiera funzionava, perché lì si passano costanti del codice, non dati salvati.
  **Regola: quando si cambia la numerazione di un enum già serializzato, si
  ricontrolla e si riassegna a mano OGNI campo di quel tipo in scene, prefab e
  ScriptableObject.** Non fidarsi di ciò che l'Inspector mostra come plausibile.
  (Gli `ItemSO` erano immuni: lì `Action` è una proprietà astratta calcolata nel
  codice, non un campo serializzato.)
- **UI: elementi che rubano i click** (lezione del 31/07/26, non un bug di codice
  ma la causa più probabile di "il tasto non risponde"). In un Canvas l'ordine dei
  figli è ordine di disegno: chi viene DOPO sta davanti e riceve il raycast per
  primo. Due casi reali già incontrati: (a) un pannello di alert con
  `Raycast Target` attivo su un Canvas superiore che copriva uno slider; (b) il
  titolo TextMeshPro del pannello Opzioni, ultimo figlio, che copriva il tasto X
  (primo figlio). Regola: decorazioni con `Raycast Target` spento, controlli
  interattivi in fondo alla lista dei figli.
- ~~**PushResolution girava mentre l'IA continuava ad agire**~~ — **RISOLTO 05/08/26,
  ma la lezione va tenuta.** `ExecuteCharge` era un metodo normale: spendeva i PA,
  faceva `SetPosition` **subito**, lanciava l'animazione e **ritornava**. `PushResolution`
  stava nella callback, quindi girava ~30 frame dopo. `PoliceAI` non la aspettava
  (a differenza di `ExecuteSkirmish`, che è `yield return StartCoroutine`), e con 5 PA
  meno i 4 della carica ne restava 1: il `while` ripartiva, trovava distanza 1 e
  infilava uno scontro **prima** che la carica fosse risolta. Se quello scontro portava
  il difensore a Morale 0, `PushResolution` girava poi su un morto — e siccome
  `Disperse()`/`Arrest()` fanno `Vacate()` ma **non azzerano `_positionCell`**,
  `ApplyPushChain` poteva **rimetterlo sulla griglia**. Col domino, insieme a lui
  tornavano su anche gli altri della catena.
  Effetto collaterale silenzioso: `PlaySkirmish` vedeva `_isMoving == true` (carica in
  corso), invocava `onComplete` e usciva — quindi `onImpact` non partiva mai e
  `RaiseCombactResult` non alzava nessun evento. Nessun errore, SFX persi.
  **Fix strutturale**: `ExecuteCharge` è `IEnumerator`, `CanCharge` è il predicato
  silenzioso separato, `StartCharge`/`ChargeWithCallback` sono l'entry point per
  `InputHandler` (stesso schema di `StartSkirmish`).
  ⚠ **Regola: chi chiama `ExecuteCharge` DEVE aspettarla.** In `InputHandler` il
  `case ActionType.Charge` finisce con `return`, non con `break`: cadere sul
  `OnActionComplete()` in fondo allo switch sbloccherebbe l'input prima della
  risoluzione, cioè lo stesso bug spostato dalla polizia al giocatore.
- ~~**PoliceAI si piantava a distanza 3 non allineata**~~ — **RISOLTO 05/08/26** dallo
  stesso fix. Il ramo era `else if (distance == 3)` secco: se `HasChargeRoom` falliva,
  `actedThisTurn` restava false e il `while` usciva — il poliziotto chiudeva il turno
  con 5 PA intatti, senza nemmeno provare ad avvicinarsi, perché il ramo `else` era
  irraggiungibile. Su esagoni l'anello a distanza 3 ha 18 celle e solo 6 sono
  allineate: succedeva **due volte su tre**. Ora la condizione è
  `else if (distance == 3 && _turnManager.CanCharge(...))`, quindi una carica illegale
  cade sull'avvicinamento.
  ⚠ Il guardiano `CanCharge` **non è opzionale**: senza, `ExecuteCharge` farebbe
  `yield break` immediato, `actedThisTurn` resterebbe true, nessun PA verrebbe speso e
  la distanza resterebbe 3 → **il turno polizia non finirebbe più**.
- **Il costo della carica vive in `TacticalQuery.ChargeCost`**, non in `TurnManager`.
  Motivo: lo legge sia `GetValidTargets` (che decide l'highlight del giocatore) sia
  `CanCharge`/`ExecuteCharge`. Erano tre literal `4` in due file. Non rimetterlo locale
  e non usare `using static`: la qualificazione esplicita è ciò che rende visibile che
  highlight ed esecuzione leggono la stessa cifra.
- **`PlayCharge` non anima il difensore, e non può.** Fino al 05/08/26 riceveva
  `defenderDestination` e `defender` e non li leggeva (rimossi). Il punto non è la
  pulizia: **quando `PlayCharge` parte, la destinazione del difensore non esiste
  ancora** — la decide `PushResolution`, che gira in `onComplete`, cioè a animazione
  finita. Qualunque parametro passato prima contiene la posizione vecchia, e col domino
  è sbagliata quasi sempre. Quando si farà la reazione al colpo per la carica, il gancio
  va dentro `PushResolution`/`ApplyPushChain`, sul modello di `onImpact` nello scontro.
- **CINQUE file non sono salvati in UTF-8** (riverificato 06/08/26 con `file --mime-encoding`
  su tutto `Assets/Script`; la voce precedente ne elencava uno solo ed era incompleta):
  `TacticalQuery.cs`, `CameraManager.cs`, `PlayFromAnyScene.cs` (ISO-8859-1) e
  `InventoryView.cs`, `UnitsSetup.cs` (cp1252). Sono byte accentati dentro commenti.
  Non rompono niente — Unity ricade sulla codepage di sistema — ma nel flusso a due
  macchine basta che un editor ne risalvi uno perché git segni righe modificate che
  nessuno ha toccato. **Da fare per primo, in un commit dedicato**, prima di aprire
  qualunque altro lavoro dal portatile.
- **Campi Bump in MovementSettingsSO sono dead code** (ChargeBumpDistance/Duration,
  SkirmishBumpDistance/Duration): definiti, esposti, mai letti da nessuno.
  NON è perché manchi l'animazione di ricezione colpo — quella ESISTE:
  `UnitMovement.PlayHitReaction`, chiamata da `ExecuteSkirmish` via il callback
  `onImpact`, e usa campi diversi (`HitReactionDistance`, `RecoilDuration`,
  `SkirmishAtkDuration`). I campi Bump sono residui di una nomenclatura
  precedente. Verificato 27/07/26.
- **TurnManager è diventato un god script** (registrato 03/08/26, refactor da fare).
  Oggi contiene: ciclo turni, carica, spinta, ricerca celle adiacenti, movimento,
  scontro, lancio, barricata, coro, sedersi, più otto canali evento serializzati.
  Sono responsabilità diverse impilate nello stesso file per comodità. Direzione
  probabile del refactor: estrarre gli esecutori per famiglia d'azione
  (`CombatExecutor`, `MovementExecutor`, `SpecialActionExecutor`), lasciando a
  `TurnManager` il solo ciclo dei turni e l'inoltro. Attenzione: `PushResolution`,
  `CalculatePushDestination` e `FoundNearCellAvailable` formano un blocco coeso —
  vanno spostati insieme. **Non farlo mentre si aggiungono feature**: il refactor
  va fatto a bocce ferme, con il gioco funzionante prima e dopo.
- ~~Dead code TurnPhases.cs / AttackOrder.cs / MovementOrder.cs~~ — **GIÀ RIMOSSI**,
  i file non esistono più sul disco (verificato 27/07/26).
- **Modello d'attacco a distanze fisse**: scontro solo a distanza 1, carica solo a
  distanza 3 in linea retta pura. Distanza 2 e distanza-3-non-allineata cadono in
  muovi+attacca. Comportamento VOLUTO, non bug — documentato per chiarezza.
- **VideoPlayer.Prepare() è veleno in Boot.unity** (27/07/26): se chiamato prima di
  Play(), il decoder gira regolarmente (time e frame avanzano fino a 82/83) ma i
  frame NON vengono presentati a schermo — si vede un solo frame — e il provider
  audio va in overflow (`AudioSampleProvider buffer overflow`). Fallimento
  SILENZIOSO: nessun errore in console, l'evento loopPointReached arriva puntuale.
  Soluzione: chiamare Play() direttamente, prepara da sé (misurato 0,05s).
  NB: la bootscene di un altro progetto chiama Prepare() e funziona, quindi è
  un'interazione con qualcosa di specifico di DISSENSO (pipeline / versione /
  render mode) — non indagata oltre perché non serve saperlo per lavorare.
  Trovato per bisezione: baseline minima funzionante (solo VideoPlayer +
  PlayOnAwake, tutto il resto disattivato), poi un elemento reintrodotto per volta.
- **VideoPlayer.length vale 0 senza Prepare()**: è proprietà del player e si popola
  solo a clip preparata. Per timeout e durate usare `_videoPlayer.clip.length`,
  che è metadato dell'asset e c'è sempre. Regola: dato statico → fonte statica.
- **Clip intro non in profile baseline**: MIDFIN_16_9  def.mp4 è H.264 Main, Unity
  logga "Unexpected timestamp values detected" e riallinea i timestamp da sé.
  Warning innocuo, playback corretto. Se un giorno desse fastidio: transcodifica
  (flag Transcode nell'Inspector del VideoClip). Il nome file ha un doppio spazio.

# DA FARE PRIMA DI PROVARE SUL SERIO

**La scala del Morale è quella vecchia e ora è sbilanciata.** Operai 2, Anarchici 3,
Black Bloc 3, Studenti 4, Pacifisti 10. Con le aure che prestano fino a 4 punti quei
numeri sono già fuori scala, e quando arriverà il panico del GDD 17.4 (shock 3/2/1)
tre gruppi su cinque morirebbero al primo urto. Proposta del cap. 17.8: Operai 6,
Anarchici 9, Black Bloc 9, Studenti 12, Pacifisti 18–24 — stesse proporzioni, ma 3
punti diventano una ferita e non una condanna.

**Il pannello How to Play è obsoleto.** Non menziona le azioni per gruppo, le aure,
la Coesione, né la differenza fra arrestato e disperso. Sono tutte regole che il
giocatore deve conoscere per giocare, non dettagli interni.

---

# PRIORITÀ DI DESIGN (analisi 03/08/26)

Analisi completa in `D:\GDDRIOT\16-Priorita-Identita-Ludica.md`. Sintesi per chi
scrive codice, perché condiziona COSA vale la pena implementare:

**Il problema.** Il pilastro di design del GDD dice: "se una regola potrebbe stare
in un gioco qualsiasi senza perdere senso, è sospetta". Oggi NESSUNA meccanica
implementata supera quel test — movimento, scontro a distanza 1, carica a distanza
3, morale che scende: tutto trasportabile in un tattico fantasy senza cambiare
niente. Il Coro è un buff ad area, Sedersi è un buff difensivo. Inoltre il gioco
non sa distinguere un'occupazione pacifica da una violenta: stesso esito, stessi
numeri. Manca il sistema che dà SIGNIFICATO alle azioni, non mancano azioni.

**Ordine di lavoro consigliato** (dettaglio e costi nel cap. 16 del GDD):
1. **Coesione** — valore su LVLManager derivato dalla dispersione delle unità, che
   alimenta il modificatore di Difesa già tabellato nel GDD 5.4. È ciò che rende il
   corteo un corteo e non una squadra, e dà senso tematico a Coro e Sedersi.
2. **Zona Rossa** — flag su HexTypeSO + regola di priorità bersaglio in PoliceAI.
   Già "deciso in design" nel GDD 5.6: nulla da progettare, solo da costruire.
3. **Contatori di violenza locali al livello + schermo di conto** — scontri
   ingaggiati, spezzoni persi, obiettivo preso con/senza violenza. Logica banale,
   il lavoro è UI. NON è l'Aggressività cross-level (quella richiede lo strato run
   che non esiste: costruirla ora è una trappola).
4. **IA polizia repressiva** (cordone, blocco, escalation) — costo alto e incerto,
   va DOPO le prime tre.

**Da non costruire adesso**: roster persistente, ComposeCorteo, Aggressività e
Repressione cross-level, campagna. Dipendono tutti da uno strato run inesistente.

---

# DA FARE (concordato)

## ORDINE DI LAVORO CONCORDATO (04/08/26, aggiornato 06/08/26)
**spinta a domino (FATTA 05/08/26) → PASSATA DI FIX (06/08/26) → panico → scena
Assemblea → refactoring obiettivi → IA polizia.**

⚠ **La passata di fix si è infilata davanti al panico ed è concordata.** Il triple
check del 06/08/26 ha trovato quattro bug attivi, uno dei quali (input del giocatore
durante il turno polizia) corrompe lo stato della griglia e ne rende raggiungibile un
altro. Scrivere il panico sopra una griglia che può essere mutata da due parti
contemporaneamente significa non poter distinguere un bug del panico da un bug
preesistente. Lista e ordine in `D:\UnityProject\GDDRIOT\FIXLIST_2026-08-06.md`;
le prime otto voci sono mezza giornata e chiudono tutto ciò che è attivo.

Gli SFX più sotto restano validi ma NON sono il prossimo passo: si fanno quando
esisteranno gli eventi di gameplay da agganciare.

### 1. Panico — design CHIUSO, codice da scrivere
Design completo in `D:\GDDRIOT\17-Coesione-Adiacenza-e-Panico.md` §17.4 e §17.6.
Riassunto operativo:
- Va in panico **chi PERDE** lo scontro di carica (simmetrico, vale anche per la
  polizia). Si propaga **per contatto** lungo le adiacenze della stessa parte:
  **-3** a chi ha perso, **-2** agli adiacenti, **-1** agli adiacenti di quelli, poi
  si spegne. Il decadimento si misura in **passi attraverso la folla**, NON in
  distanza esagonale — è quello che fa contare la forma del corteo.
- Perdita di Morale **una tantum**, all'ingresso. Ordine obbligato: prima lo shock
  con le aure ancora attive, **poi** si tolgono le aure e si tronca al nuovo
  massimale. L'ordine inverso fa pagare due volte.
- Durante il panico l'unità **non dà e non riceve aure**. Si muove e agisce
  normalmente (versione permissiva, si stringe solo se serve).
- **Durata: 3 turni il corteo, 1 turno la polizia** (loro sono organizzati, si
  riformano). Si contano i **turni di polizia**, decremento in un **punto unico**:
  `ExecutePoliceTurn`, dove già si ricaricano i PA degli spezzoni.
- **Seduto = frangifuoco**: non entra in panico e **interrompe la catena**.
- **Chi è in panico NON può sedersi.** Senza questa regola siediti+rialzati (3 PA)
  azzera tre turni di panico. Il Coro resta l'unica cura anticipata.
- Serve la **visualizzazione**: sull'unità (`UnitStatusView`, già esistente) **e**
  come testo nel pannello unità. Fa coppia con l'indicatore di unità seduta, che
  manca anch'esso.

~~**DA FIXARE PRIMA DI SCRIVERE IL PANICO**: `TurnManager.PushResolution`~~ —
**CHIUSO 05/08/26.** I bracci invertiti del ramo `Win` erano già stati sistemati da
Edoardo nel working tree; le graffe del ramo `Lose` non erano mai state rotte (solo
indentate male). Poi l'intero metodo è stato riscritto per la **spinta a domino** (vedi
sezione dedicata) e la corsa fra carica e turno IA è stata chiusa rendendo
`ExecuteCharge` una coroutine (vedi bug noti). I due log bugiardi
(`"Police Disperse"`, `"...arrested on..."`) sono spariti con la riscrittura.
*Nota di metodo, due volte in una sessione: questa voce descriveva uno stato superato da
una modifica non committata. Prima di fidarsi di una voce "da fixare", `git diff` sul file.*

**Il terreno per il panico adesso è pronto.** `PushResolution` decide `Win`/`Lose`/`Par`
in un punto solo, la catena di adiacenze è già percorsa da `TryBuildPushChain`, e
`ExecuteCharge` non ritorna prima che la risoluzione sia avvenuta — quindi il panico può
essere agganciato senza domandarsi se lo stato che legge sia quello giusto.

⚠ **Prima di scrivere il panico va decisa una cosa lasciata aperta**: `CombatResolver.Resolve`
gira **dopo** che l'attaccante si è già spostato (`atk.SetPosition(destinationCell)` precede
`PushResolution`, che sta nella callback). Quindi le aure dell'attaccante sono calcolate
dalla cella d'arrivo, non da quella di partenza: chi carica in profondità arriva **solo** e
combatte senza il bonus dei suoi. Le due letture sono entrambe difendibili — "l'impeto viene
dal gruppo dietro" contro "all'urto sei solo" — ma oggi è un effetto collaterale
dell'ordine delle righe, non una scelta. E decide chi perde, quindi **chi va in panico**.
Va deciso apposta, e l'highlight deve dire la stessa cosa dell'esecuzione.

⚠ **Il domino e il panico si sovrappongono, tenerlo d'occhio.** Stesso evento scatenante,
stessa propagazione per contatto, stesso istante; il panico si irradia in tutte le
direzioni, il domino solo sull'asse della spinta. Nel gioco da tavolo *Corteo* (1979) sono
**un sistema solo**: il grappolo che va in panico si sposta di un esa a scelta
dell'avversario, e chi non può spostarsi è arrestato. Se in playtest i due si pestano i
piedi, quella è la strada già percorsa da altri.

⚠ **La scala del Morale va alzata insieme al panico**, non prima: i valori attuali
(Operai 2, Anarchici 3, BB 3, Studenti 4, Pacifisti 10) contro uno shock da 3
uccidono tre gruppi su cinque al primo urto. Proposta cap. 17.8: 6/9/9/12/18-24.
Edoardo vuole tararli **testandoli col panico**.

### 2. Scena Assemblea — quattro prerequisiti mancanti
Composizione del corteo prima del livello: 1000 punti fissi, roster di 3 unità per
gruppo politico, equipaggiamento comprato per il corteo e assegnato alle unità.
Non esiste ancora nulla di: campi costo sugli SO, inventario a livello di corteo,
passaggio di stato fra scene, istanziazione a runtime delle unità.

### 3. Obiettivi — design chiuso e PARCHEGGIATO
`D:\GDDRIOT\19-Obiettivi-e-Occupazione.md`. Occupazione per turni consecutivi,
obiettivo rivendicato che non paga più, obiettivi configurabili. Non lavorarci ora.

## Poi: SFX — il blocco è mezzo sciolto (aggiornato 06/08/26)
Il sistema audio è pronto e testato. **Il punto 1 qui sotto è già stato fatto lato
codice**: `TurnManager` dichiara e alza `_skirmishWin/Lose/Par` (in `RaiseCombactResult`,
dentro `onImpact`) e `_chargeWin/Lose/Par` (in `RaiseChargeResult`, dentro
`PushResolution`). **Ma tutti e sei gli slot sono vuoti in scena** (`fileID: 0`),
quindi oggi non parte niente e non lo si vede perché sono alzati con `?.`.
Restano senza evento: movimento, coro, sedersi, lancio, barricata, dispersione.
Ordine di lavoro:
1. ~~Aggiungere i campi `[SerializeField] GameEventSO` in `TurnManager` e alzarli~~ —
   **FATTO per scontro e carica.** Manca solo **creare i tre asset della carica**
   (`ChargeWinEvent`, `ChargeLoseEvent`, `ChargeParEvent`) e **collegare i sei slot
   nell'Inspector**, riusando i tre orfani già a disco per lo scontro.
2. Creare i canali mancanti (movimento, coro, sedersi, lancio, barricata,
   dispersione) come asset in `ScriptableObjects/Events/`, e i campi corrispondenti.
3. Solo allora creare gli `SFXSO` e infilarli nell'array `_sfxevents`. Il
   collegamento è tutto da Inspector, zero codice nuovo.
Nota di design: conviene un evento per *esito* (Win/Lose/Par) più che per *azione*,
così lo stesso scontro può suonare diverso a seconda di come va.
⚠ Licenze: freesound NON è tutto CC0, è un misto CC0/CC-BY/CC-BY-NC. "Placeholder"
non è una categoria che esiste nel diritto d'autore: se la build è pubblica,
l'attribuzione scatta. Filtrare solo CC0 e tenere la lista fonti da subito.

## Correzioni rapide già individuate
4. ~~`AudioManager.Awake`: `&&` → `||`~~ — **FATTO** (verificato 06/08/26).
5. ~~`SFXSO`: togliere `using UnityEngine.LightTransport;`~~ — **FATTO**.
6. ~~Togliere i `Debug.Log` diagnostici `[AUDIO]`~~ — **FATTO**.
7. ~~`GameManager.OnApplicationQuit()` → `QuitGame()`~~ — **FATTO**.
8. Separare i tre AudioSource in GameObject figli distinti (`MusicSource`,
   `SFXSource`, `VideoSource`) sotto l'AudioManager: oggi sono componenti impilati
   sullo stesso oggetto, indistinguibili nell'Inspector, e il VideoPlayer della
   bootscene condivide una source con la musica. **ANCORA APERTA.**

## Rifiniture rimandate consapevolmente (chiedere sempre quando si fa il punto)
- ~~**`PoliceAI._onSelectedEvent` punta a `SelectedUnitsEvent`**~~ — **RISOLTO**: in
  `LVLTest.unity` il campo punta a `SelectedPolice` (guid `93aeb65f…`, verificato nel
  YAML il 06/08/26). Resta da valutare il secondo canale sul `CameraManager` per non
  perdere il follow durante il turno polizia.
- **PoliceAI non cerca bersagli alternativi**: se lo scontro più vicino è perdente
  rinuncia e resta ferma, invece di valutare un altro spezzone o riposizionarsi.
- **Tre Operai adiacenti sono imbattibili** (Def 8 + 2 di aura a testa contro Atk 8
  della polizia). Non è un bug dell'IA: è l'assenza del tetto alle adiacenze del
  GDD 17.8, che ora ha un caso concreto a supporto.
- **Nessun indicatore visivo di unità seduta.** Il sedersi è diventato una scelta
  tattica centrale (blocca le cariche, ancora la formazione contro il panico), ma
  sulla griglia non si distingue chi è seduto da chi è in piedi — quindi non si può
  pianificare. Rimandato da Edoardo il 03/08/26 alla fase di rifinitura.
  ⚠ **Dal 05/08/26 pesa di più**: un seduto interrompe la catena del domino, quindi
  può far arrestare chi gli sta davanti. È l'unica regola del gioco in cui una scelta
  di un'unità uccide un'altra unità, e oggi non è visibile sulla griglia. Se in
  playtest qualcuno perde uno spezzone senza capire perché, la causa è questa.
  Vie possibili, dalla più economica: riga o icona "SEDUTO" nel pannello unità (dice
  anche perché la Difesa mostra un numero più alto, visto che il +5 da seduto è dentro
  il valore base); schiacciamento del `graphicsTransform`; sprite dedicato — la
  soluzione giusta, ma dipende dalla direzione artistica.
- **Il pannello statistiche non si aggiorna quando cambia il vicinato.** `Refresh`
  scatta alla selezione e ai cambi turno, non quando un'altra unità si sposta: se
  muovi un Operaio accanto allo spezzone selezionato, il bonus d'aura mostrato resta
  vecchio finché non deselezioni. Si risolve chiamando `Refresh` dallo stesso punto in
  cui si ricalcolerà la Coesione (dopo movimento, spinta, dispersione).
- **Il bonus da seduto è invisibile nel pannello**: `SpezzoneRuntime.Def` restituisce
  `Def + 5` da seduto, quindi il +5 è dentro il numero base e non è distinguibile
  dall'aura. Per separarlo servirebbe scomporlo alla fonte.

## Arretrato precedente
9. Animazione ricezione colpo del difensore: FATTA per lo scontro
   (`PlayHitReaction` via `onImpact` in ExecuteSkirmish). MANCANO la versione per
   la carica e la distinzione win/lose. Verificato 27/07/26.
10. Animazione scontro polizia (stessa logica attaccante).
11. Riprodurre e fixare il bug muovi+attacca combinato lato AI (lato player è già
    chiuso da `GetAttackOption`).
12. ~~Azioni per tipo di unità~~ — **FATTO 03/08/26**. `ActionType` è `[Flags]`,
    `UnitsSO._allowedActions` è la maschera, il filtro è in
    `InputHandler.SetSelectedAction`. Maschere attuali: Anarky 3, BlackBlock 7,
    Operai 13, Pacifisti 25, Studenti 27, Police 0. Manca solo l'ingrigimento dei
    bottoni non disponibili nel pannello azioni (serve agganciare
    `ActionButtonPanel` a `SelectedUnitsEvent`).
13. Impedire la barricata sulle celle obiettivo: `ExecuteBarricade` non guarda
    `cell.Type.IsObjective`.
(Il pannello How to Play è FATTO: contenitore e testo, confermato da Edoardo il
03/08/26. Il Documento di Progetto lo dava ancora come "testo non inserito" —
quella voce è obsoleta.)

## Changelog sessione 29 (06/08/26) — triple check, nessuna riga di codice toccata
Sessione dal portatile. Working tree pulito su `518220952`; niente è stato modificato
nel codice, solo letto e documentato.

- 📖 **Dump completo dei 67 script** in `D:\UnityProject\GDDRIOT\DISSENSO_SourceDump_2026-08-06.md`,
  con contesto, regole architetturali e lista "già noto" in testa. Serve per far
  rileggere il progetto da revisori esterni senza doverglielo rispiegare ogni volta.
- 📖 **Revisione incrociata a tre** (Claude sul repo e sulla scena, ChatGPT e DeepSeek
  sul dump). Risultato in `D:\UnityProject\GDDRIOT\FIXLIST_2026-08-06.md`: 4 bug attivi,
  1 attivo solo in combinazione, 11 rischi latenti, 8 questioni di design.
- 🔴 **Nove voci del documento erano più vecchie del codice** e sono state corrette qui
  (audio, `QuitGame`, `_onSelectedEvent`, `_turnManager`, `ActionSlotUI`, eventi di
  combattimento, `CanCharge`, file non-UTF8, `SelectionOutline`).
  La peggiore era **"`TurnManager.CanCharge` è dead code, sicura da cancellare"**: vera
  il 27/07, falsa dal 05/08, e seguirla avrebbe rotto il turno polizia.
- 🔴 **Correzione di una mia diagnosi**: avevo classificato `ExecuteSitStand` /
  `ExecuteBarricade` senza `RefreshBoardState` come bug attivo. È solo un'omissione
  architetturale — il pannello si aggiorna comunque via `_unitSelectedEvent`, e quelle
  due azioni non cambiano le adiacenze.

**Tre lezioni di metodo che valgono oltre questa sessione:**

1. **Una voce "già noto / sicura da cancellare" invecchia più in fretta del codice.**
   Tre voci su nove erano vere quando scritte. Prima di agire su una voce del documento,
   un grep. Prima di fidarsi di una voce "da fixare", `git diff`.
2. **Due bug innocui separati possono essere un bug grave insieme.** L'input durante il
   turno polizia e `TryOccupy` che ignora le barricate sono entrambi non sfruttabili da
   soli; composti mettono un poliziotto sopra una barricata. Un difetto di
   sincronizzazione non è mai *solo* di sincronizzazione: annulla tutti i controlli che
   qualcun altro ha dato per garantiti a monte.
3. **Un revisore che sbaglia i numeri di riga sbaglia anche il resto.** DeepSeek citava
   righe a memoria (`TryBuildPushChain` a 305 invece di 169) e ha prodotto due
   affermazioni sicure e false, fra cui "questo codice non compila" su codice che
   compila. ChatGPT citava righe esatte e, messo alla prova, ha separato da solo quello
   che sapeva da quello che stava deducendo. **La precisione delle citazioni è il
   predittore migliore dell'affidabilità del resto.**

## Changelog sessione 28 (05/08/26) — spinta a domino e carica asincrona
- 🟢 **Spinta a domino**: `CalculatePushDestination` + `FoundNearCellAvailable` rimossi,
  sostituiti da `TryBuildPushChain` / `ApplyPushChain` / `ResolvePushOrRemove`. Catena
  senza tetto, muro su nemico/obiettivo/seduto/barricata/bordo, catena bloccata → esce
  chi ha perso lo scontro. Vedi la sezione "Spinta a domino".
- 🟢 **`ExecuteCharge` è diventata `IEnumerator`**, con `CanCharge` (predicato silenzioso)
  e `StartCharge`/`ChargeWithCallback` (entry point per `InputHandler`). Chiude la corsa
  fra la carica e il resto del turno IA, e con essa la resurrezione di unità già uscite.
- 🟢 **`PoliceAI` non si pianta più a distanza 3 non allineata**: `CanCharge` nella
  condizione fa cadere la carica illegale sul ramo dell'avvicinamento.
- 🟢 **Costo della carica accentrato** in `TacticalQuery.ChargeCost`: erano tre literal
  `4` sparsi fra due file, uno dei quali decideva l'highlight.
- 🟢 **`PlayCharge` ripulita** dei tre parametri che non leggeva.
- 🔴 **Correzione del documento (due volte)**: la voce "PushResolution ha i bracci
  invertiti" descriveva codice già corretto nel working tree, e le graffe del ramo
  `Lose` non erano mai state rotte. Entrambe erano sopravvissute perché rilette invece
  che riverificate. `git diff` prima di fidarsi.
- 📖 **Letto il regolamento di *Corteo* (1979)**, il gioco da tavolo di riferimento:
  la ritirata è di una sola unità, e a propagarsi per contatto è il **panico**, che
  *è* lo spostamento. Chi non può ritirarsi — per ritirata o per panico — è arrestato.
  Nessun tetto, nessun decadimento. Il panico della polizia dura fino alla loro fase
  di spostamento successiva, cioè un turno: la stessa asimmetria decisa in GDD 17.4
  senza conoscerla.

## Changelog sessione 26 (03/08/26) — bootscene e audio
- 🟢 **Bootscene completa**: video intro → fade bianco → caricamento asincrono →
  fade a nero → MainMenu, con fail-safe a tempo su ogni attesa.
- 🟢 **`PlayFromAnyScene`** (Editor): Play da qualsiasi scena parte da Boot e poi
  carica la scena di partenza.
- 🟢 **Sistema audio completo**: AudioManager persistente, mixer a tre canali,
  SFXSO, musica per scena, persistenza dei volumi.
- 🟢 **Pannello Opzioni funzionante** in MainMenu e in LVLTest.
- 🟢 **TurnManager**: `ExecuteThrow` / `ExecuteBarricade` / `ExecuteMovement`
  passati al pattern "verifica tutto, poi applica tutto" — niente mutazioni prima
  che tutti i controlli passino, niente rollback manuale. Corretto un doppio
  consumo di PA in `ExecuteMovement`.
- 🔴 **Correzione del documento**: la voce "GameManager.instance è un singleton
  statico" era falsa (vedi bug noti). Il pannello Opzioni del MainMenu non è più
  un bottone morto.

# Dipendenze Unity
- com.unity.feature.2d 2.0.1
- com.unity.render-pipelines.universal 17.0.3
- com.unity.inputsystem 1.13.0
- DOTween (animazione)
