# CLAUDE.md

## Progetto
DISSENSO (ex RIOT) — gioco tattico a turni 2D in Unity 6000.4.5f1 (URP).
Il giocatore comanda un corteo politico (spezzoni) su una griglia esagonale flat-top contro forze di polizia.
Lingua team: italiano. Commit e commenti in italiano. Nomi variabili/classi in inglese.

## Gli altri documenti (allineati 13/08/26)
- **Documento di Progetto**: `D:\GDDRIOT\RIOT_Project_Document_v29.md` — stato del codice
  (Sezione 0) e cronologia per sessione. Le versioni vecchie stanno in
  `D:\GDDRIOT\Archivio\`.
  ⚠ **Il v26 non esiste e non va cercato**: creato il 05/08 e sovrascritto dal v27 senza
  passare dall'archivio (errore, confermato il 13/08). Nessun contenuto perso — il
  changelog della sessione 28 è dentro il v27 e quindi dentro il v28. Il salto v25 → v27
  nell'archivio è atteso.
  ⚠ **Da sess.29 il changelog per sessione sta nella Sezione 0** del Documento di
  Progetto (blocchi "Novità sess.NN"), non più in fondo nell'elenco delle sessioni, che
  si ferma alla 28. Due posti per la stessa cosa: da riunificare.
- **GDD**: `D:\GDDRIOT\` numerati 00-**20**. Il **cap. 20 (Assemblea e Volantino)** è nuovo
  del 13/08/26, e il **cap. 8 (Polizia)** è stato riscritto lo stesso giorno: la polizia
  passa da inseguitore a **presidio** con guinzaglio, allarme locale e regole d'ingaggio
  per fascia di Repressione. Sono i due capitoli da leggere per primi se si riprende in
  mano il design.
- ⚠ Fino al 13/08/26 questo file citava i documenti sotto `D:\UnityProject\GDDRIOT\`,
  percorso che non esiste. Cinque riferimenti corretti in una volta.

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
  l'animazione mostra uno stato già risolto.

  🔴 **NON è un vincolo rigido, e fino al 20/08/26 questo file diceva che lo fosse.**
  Trovato da una revisione esterna che aveva ricevuto l'istruzione di controllare le
  premesse del documento. Lo stato reale sono **tre tempi diversi**:
  | azione | quando cambia lo stato logico |
  |---|---|
  | scontro, coro, sedersi, lancio, barricata | **prima** dell'animazione ✅ |
  | movimento | **durante**: `UnitMovement.MoveCoroutine` chiama `SetPosition(cell)` dopo l'animazione di *ogni passo* |
  | carica | **spezzata**: l'attaccante si sposta prima, la spinta del difensore è risolta da `PushResolution` nella callback di fine animazione (o al timeout) |

  ⚠ **L'eccezione del movimento è deliberata e va tenuta**: la cella era libera quando il
  percorso è stato calcolato, e occupare in anticipo significherebbe prenotare celle dove
  non sei ancora arrivato. `SetPosition` restituisce `false` e il movimento si interrompe.
  È la stessa ragione per cui `MoveCoroutine` usa il valore di ritorno.

  ⚠ **La carica invece è intreccio, non scelta**, ed è il motivo per cui il progetto non
  può ancora far girare le regole senza Unity: finché la posizione logica cambia dentro un
  tween, non esiste un confine "regole prima, rappresentazione dopo" da cui tagliare.
  *Regola che ne esce: la formula giusta è "**la logica non dipende mai da cosa mostra
  l'animazione**", che è vera ovunque. "Prima" è vero solo per alcune azioni, e chi ci
  costruisce sopra dando per scontato il resto sbaglia.*

## Resolver, servizi e test (RIFATTO 22/08/26) — la struttura è cambiata
`Core/Resolver/`, `Core/Services/`, `Test/Editor/`, `Units/Visualization/UnitActionPresenter.cs`.
Il progetto è passato da **74 file / 7.302 righe** a **98 / 11.322**, e ~4.000 righe sono
strato nuovo, non feature: **82 test in 14 file** e l'estrazione delle regole dagli esecutori.

**Il nuovo strato, e chi fa cosa:**

| classe | tipo | responsabilità |
|---|---|---|
| `PushResolver` | statica pura | catena, sfogo laterale, rimozione. Restituisce un `PushResult` (`IsResolved`, `WasRemoved`, `Moves`) |
| `PanicResolver` | statica pura | onda di panico → lista di `PanicEffect(Unit, Steps, PanicTurns)` |
| `ChargeResolver` | statica pura | `CanStart(...)` — legalità della carica |
| `CombatResolver` | statica pura | `ResolveSkirmish` → `SkirmishResolution`, più `Resolve`/`GetEffectiveAtk`/`GetEffectiveDef` |
| `ItemActionResolver` | statica pura | `ResolveThrow` / `ResolveBarricade` → `ItemActionResult` |
| `UnitActionResolver` | statica pura | `ResolveChant` / `ResolveSitStand` → `UnitActionResult` |
| `AuraService` | statica pura | `Resolve` → `AuraResult` |
| `CohesionService` | statica pura | `Calculate` |
| `UnitActionPresenter` | classe C# (non MonoBehaviour) | animazioni di scontro e carica, con timeout a 5s |

⚠ **`CombatResolver` si è spostato** da `GenericStatic/` a `Core/Resolver/`.

⚠ **I resolver non contengono NESSUN `Debug.Log`.** Restituiscono un esito tipizzato
(`UnitActionFailure`, `SkirmishFailure`, `ItemActionFailure`) e chi logga è il chiamante.
Non rimetterci dentro la diagnostica: è ciò che li tiene collaudabili senza avviare il gioco.

### Il buco del timeout sulla carica è chiuso PER COSTRUZIONE
Prima `PushResolution` stava nella callback di fine animazione, e allo scadere del timeout la
carica risultava pagata e mai risolta — cosa tamponata con `ResolveOnce()` e un flag `resolved`
perché una callback in ritardo l'avrebbe fatta girare due volte. Adesso:

```csharp
yield return _actionPresenter.PlayCharge(atk, def, destinationCell);
PushResolution(atk, def);
```

`PushResolution` sta **dopo lo yield**: gira sempre, esattamente una volta, timeout o no.
⚠ **`ResolveOnce` e il flag `resolved` non esistono più e non vanno reintrodotti**: il percorso
che li rendeva necessari è sparito. Stessa cosa per `FinalizeOnce` nello scontro.

### Validazione della configurazione all'avvio
`LVLManager.ValidateReferences()` (chiamata in `Awake`) + `TurnManager.CollectConfigurationErrors(...)`.
Raccolgono **tutti** gli errori in una lista e li stampano in **un solo `LogError`**, poi
`LVLManager` fa `_gameOver = true; enabled = false;` e `TurnManager` si disabilita in `Start`.
`_isConfigured` è letto in `Awake`/`OnEnable`/`OnDisable`/`Start` di entrambi.

⚠ **L'ordine regge per costruzione**: `TurnManager._isConfigured` è scritto da
`LVLManager.Awake`, e Unity garantisce che *tutti* gli `Awake` girino prima di *qualunque*
`Start`. Se `LVLManager` è disattivato, `_isConfigured` resta `false` e `TurnManager` si spegne
da solo — fallisce chiuso.

### I test
`Test/Editor/`, EditMode, 82 test. **Non sono test dell'implementazione: asseriscono le regole
documentate.** Esempi da `PanicWaveTests`: *il seduto è frangifuoco*, *la propagazione si ferma
a `PanicSteps`*, *il panico della polizia non scende sotto 1 turno*, *un'ondata debole non
accorcia un panico più lungo*, *`GetPanicWave` non muta niente*.
⚠ **Se cambi una regola e un test diventa rosso, il test ha ragione finché non aggiorni anche
la documentazione.** È l'unica rete che il progetto abbia mai avuto contro le regressioni.
⚠ Non esiste nessun `.asmdef`: i test compilano nell'assembly Editor. Funziona, ma non c'è
confine e non girano fuori dall'Editor. Due asmdef serviranno il giorno della CI.

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

## Il pannello unità esiste in DUE copie (scoperto 10/08/26)
In `LVLTest.unity` ci sono **due** `UnitStatsPanelView`: `SpezzoneSelectedPanel` e
`PoliceSelectedPanel`. Chi aggiunge un campo serializzato deve assegnarlo **su entrambi**.

⚠ **Un campo dimenticato su uno dei due ha bloccato il gioco.** Aggiunto `_statusText` e
assegnato solo sul pannello spezzoni, all'inizio del turno polizia `PoliceAI` alza
`_onSelectedEvent` → il pannello polizia fa `Show` → `Refresh` → `NullReferenceException`.

E siccome `Raise` è **sincrono**, l'eccezione risale fino alla coroutine di `PoliceAI` e la
uccide a metà: la polizia non agisce, `ExecutePoliceTurn` non finisce, `_waitingForPolice`
resta `true`, e con `CanAcceptPlayerInput` l'input resta bloccato per sempre.

**Un campo non assegnato in un pannello UI ha fermato la partita.** È la stessa fragilità
chiusa col pattern `_isValid`, rientrata da una porta laterale. Da qui due regole:
- i widget opzionali del pannello si leggono con un null-check (`if (_statusText != null)`),
  come già si fa per `_aptBar` e `_aptValueText`;
- **non** metterli in `_isValid`: quello impedirebbe l'iscrizione e un pannello muto è
  peggio di un pannello senza una riga.

🔴 **Il fix del 10/08 era a metà, ed è stato scoperto solo il 16/08 da una revisione
esterna.** La guardia `if (_statusText != null)` era stata **aggiunta**, ma otto righe più
sotto era rimasta l'assegnazione originale **senza guardia**:

```csharp
if (_statusText != null)
    _statusText.text = DescribeStatus(_currentUnit);   // riga 125-126
...
_statusText.text = DescribeStatus(_currentUnit);       // riga 133, non protetta
```

Quindi per sei giorni il pannello è stato **esattamente vulnerabile come prima**, con
l'aggravante che il codice sembrava protetto. Chiuso il 16/08 cancellando la riga 133.

⚠ **Regola che ne esce, e vale ovunque: un fix che aggiunge una guardia deve RIMUOVERE il
percorso non protetto.** Aggiungere l'`if` sopra e lasciare l'accesso sotto non è metà
lavoro, è zero lavoro travestito da uno.

`DescribeStatus` restituisce **stringa vuota** per lo stato normale, non "NORMAL": lo stato
normale è assenza di informazione, e scriverlo insegna al giocatore a ignorare quella riga
proprio quando serve.

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

## Stato duplicato fra InputHandler e OrderPreviewRenderer (08/08/26)
Il refactor B3/B7 ha dato al renderer una copia di `_selectedItem` e `_currentAction`,
perché per disegnare l'highlight di Lancio e Barricata gli serve il costo dell'oggetto.
Due copie dello stesso stato: entrambe le regressioni trovate nel triple check nascono
da lì, e chi tocca una deve toccare l'altra.

- **Cambiando azione, l'oggetto va scartato se non serve alla nuova**
  (`if (_selectedItem.Action != action) _selectedItem = null;`). Va fatto **in tutti e
  due i file**. Senza, scegli una barricata, premi Lancio, e la query non trova niente
  senza che il giocatore possa capire perché.
- ⚠ **`_itemSelectedEvent` significa due cose diverse**: "oggetto cliccato" (lo alza
  `InventoryView` a ogni clic) e "oggetto accettato" (`InputHandler` può rifiutarlo con
  `CanAcceptPlayerInput`). Chi ascolta non può distinguerle. Il renderer accettava clic
  che `InputHandler` aveva scartato, e i due stati si separavano — riaprendo la
  divergenza highlight/esecuzione da un'altra porta. Toppa attuale: il renderer
  ridisegna **solo se `item.Action == _currentAction`**. Regge finché gli ascoltatori
  sono due; al terzo, la strada giusta è che `InputHandler` ri-annunci la selezione
  accettata su un canale suo.
- **L'idempotenza al posto di un ordine imposto**: `OnActionSelected` e `OnItemSelected`
  arrivano in ordine non garantito (`EventChannelSO.Raise` itera all'indietro, quindi
  vince chi si è iscritto per ultimo). Entrambi aggiornano il proprio pezzo di stato e
  chiamano `RefreshActionHighlight()`: l'ultimo ad arrivare ha l'informazione completa.
  Non imporre un ordine — mantenere l'idempotenza.

## Chi parla col giocatore (stabilito 08/08/26)
Regola nata chiudendo B3/B7, quando l'alert cominciò a dire "not valid Target" al posto
di "Not enough AP".

- **`_alertEvent` appartiene a `InputHandler`**, che è l'unico che sa cosa il giocatore
  stava provando a fare. `InputHandler.DescribeInvalidTarget` traduce un rifiuto in una
  frase. Gli alert dentro `TurnManager` restano come rete per chiamanti che non sono il
  giocatore (IA, panico, tasti di test): il giocatore non li vede quasi mai, perché la
  query rifiuta prima.
- **Forma obbligatoria: una decisione, tante spiegazioni.** Un solo `if` decide
  (`GetValidTargets`, `CanThrow`, `CanPlaceBarricade`); i rami sotto scelgono solo il
  messaggio. Se una spiegazione si disallinea dalla decisione, il danno massimo è un
  testo impreciso — mai un'azione eseguita a torto o rifiutata a torto. È la differenza
  fra duplicare una decisione e duplicare una descrizione.

## La carica non confronta più Atk e Def (deciso 08/08/26)
Cambio di design. `PushResolution` non chiama più `CombatResolver`: **chi subisce la
carica viene spinto, punto.** L'unica eccezione è il seduto, già filtrato da `CanCharge`.
Spariti `RaiseChargeResult` e i tre canali Win/Lose/Par, sostituiti da un solo
`_chargeEvent`. `CombatResolver.Resolve` resta usato solo da `ExecuteSkirmish`.

**I due ruoli si separano**: lo scontro logora e vuole statistiche favorevoli, la carica
sposta e funziona sempre. Un muro non si sfonda a spallate, lo si attraversa caricando.

Tre problemi che si chiudono insieme:
- **Gli Operai imbattibili** (Def 8 + 4 di aura contro Atk 8): erano invulnerabili a
  *entrambe* le azioni perché entrambe passavano dallo stesso confronto.
- **Il panico torna a "chi subisce la carica"**, com'era nella prima stesura del GDD 17.4.
  Era stata scartata perché una carica fallita sarebbe stata gratis: adesso non esiste
  una carica fallita, e comunque costa 4 PA e toglie 1 di Morale.
- ⚫ **La decisione E1 evapora.** "Le aure dell'attaccante in carica si calcolano dalla
  cella d'arrivo" era aperta solo perché decideva chi *vince* la carica. Senza confronto,
  la domanda non ha più oggetto. Era il prerequisito del panico.

⚠ **Da fare, non ancora fatto: `PoliceAI` non sa della nuova regola.** A distanza 1 fa
ancora `if (atk <= def) break;` e rinuncia al turno invece di arretrare e caricare. Il
muro di Operai è quindi risolto nelle regole ma non nel comportamento. Va insieme alla
voce "PoliceAI non cerca bersagli alternativi".

⚠ **Da decidere: chi può caricare.** Oggi tutti — le maschere `_allowedActions` sono
Anarky 3, BlackBlock 7, Operai 13, Pacifisti 25, Studenti 27, Police 1, e il bit `Charge`
(=1) c'è in tutte. Un Pacifista che carica la polizia è tematicamente storto: basta
portare la sua maschera a **24**. Da valutare insieme al resto del bilanciamento.
*(Nota: `CanPerformAction` è controllato solo in `InputHandler.SetSelectedAction`, quindi
la maschera della polizia oggi non ha effetto — `PoliceAI` non la guarda.)*

## Debito registrato l'08/08/26
- **I costi in `TacticalQuery` sono `const`.** Immutabili, ma **non tarabili**: non si
  vedono in Inspector e cambiarli richiede una ricompilazione. Per numeri strutturali
  (gittata del lancio = 2) va bene; per `ChargeCost` e `ChantCost`, che sono manopole di
  bilanciamento, andranno in uno ScriptableObject sul modello di `MovementSettingsSO`.
  Da fare alla passata di bilanciamento, non prima.
  *(Nota: `SitCost`, `StandCost` e `ThrowRange` sono `private` di proposito — nessuno
  fuori da `TacticalQuery` deve conoscerli. Chi vuole sapere quanto costa alzarsi chiama
  `GetSitStandCost(unit)`.)*
- **Le 35 celle obiettivo sono volute** (sparse nella mappa, verificato da Edoardo
  l'08/08/26). Vanno riviste quando si farà l'**occupazione temporanea** del GDD cap. 19,
  perché quella cambia cosa significa "stare su un obiettivo". Non prima.

## Naming Convention
⚠ **`[Header]`, `[Tooltip]` e i messaggi di log vanno scritti in INGLESE** (deciso
16/08/26). I commenti restano in italiano. Il codice esistente è misto: è previsto un
passaggio di uniformazione, non ancora fatto — nel frattempo **il nuovo si scrive già in
inglese**, per non allargare il lavoro di quella passata.
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

## Panico (IMPLEMENTATO 08/08/26) — GDD cap. 17.4
`AbstractUnitsRunTime._panicTurnsLeft` + `TacticalQuery.GetPanicWave` +
`TurnManager.ApplyPanicWave` + `UnitMovement.SetPanicVisual`.

- **Non è uno stato di `UnitsStatus`**: un'unità in panico è **viva**, si muove e agisce.
  È un campo a parte, con `IsPanicked => _panicTurnsLeft > 0`. Metterlo nell'enum avrebbe
  fatto sì che ogni `IsAlive` del progetto la considerasse morta.
- **Chi subisce la carica**: −1 Morale (causa `CauseFrom(atk)`, quindi arresto se l'ha
  caricato la polizia) e 3 turni di panico. **La propagazione non toglie Morale a nessuno**,
  porta solo lo stato: il gradiente è sulla durata (3 / 2 / 1 per passo).
- **`GetPanicWave` è una BFS pura** in `TacticalQuery`: riceve una **cella** e l'unità
  epicentro, restituisce `(unit, steps)` e non muta niente. L'origine è una cella e non
  un'unità perché **chi ha subito la carica può essere già uscito di gioco**: il corteo
  l'ha visto cadere lo stesso.
- **Propagazione per contatto, non per raggio**: le celle vuote non entrano nella coda,
  quindi due unità a distanza 2 separate da una cella vuota non si contagiano. È ciò che
  fa contare la **forma** del corteo. Massimo 2 passi (`PanicSteps`).
- **Il seduto è frangifuoco**: non entra in panico e non trasmette.
- **Chi è già in panico non ha trattamento speciale**: entra nell'onda come tutti (la
  durata si aggiorna col `Mathf.Max` dentro `ApplyPanic`) e l'onda **lo attraversa**,
  occupando il suo passo. Che non paghi due volte è garantito dal fatto che solo il
  passo 0 perde Morale.
- **Durata**: corteo `Mathf.Max(1, 3 - steps)` = 3/2/1, polizia = **1/1/1** (con la
  sottrazione secca sarebbe 1/0/-1, cioè nessuna propagazione). Scelta implicita che il
  GDD non esplicitava.
- **Decremento a fine turno della propria parte**: spezzoni in `EndTurn`, polizia in
  `ExecutePoliceTurn` — cioè dove si ricaricano i PA di quella parte. ⚠ Il "punto unico"
  del GDD era sbagliato di un turno intero.
  ⚠ **`RefreshBoardState()` dopo il decremento non è opzionale**: uscire dal panico
  rimette in circolo le aure, e senza ricalcolo resterebbero spente per sempre.
- **In panico non si danno né si ricevono aure**, e la regola vive **tutta dentro
  `TacticalQuery.GetAuraBonus`**, in due punti che vanno letti insieme:
  ```csharp
  if (unit.IsPanicked) return total;          // non RICEVE
  ...
      if (neighbor.IsPanicked) continue;      // non DÀ
  ```
  ⚠ **Fino all'08/08 sera c'era solo il secondo**, e mezza regola non veniva applicata:
  il Morale funzionava (passa da `ApplyAuras`, che gestiva a parte il ricevente) ma
  **Atk e Def no**, perché `CombatResolver.GetEffectiveAtk/Def` chiama `GetAuraBonus`
  al volo. Uno spezzone in panico continuava a difendersi col bonus dei compagni.
  Non si vedeva perché l'effetto vistoso — il Morale — era corretto.
  Lezione: quando una regola ha due versi, **cercare esplicitamente il secondo**.
- Dopo il fix, `LVLManager.ApplyAuras` chiama `GetAuraBonus` liscia: il ternario
  `IsPanicked ? 0 : ...` è stato tolto perché duplicava la regola in un secondo posto.
- **È così che il panico uccide**: non colpisce, toglie il sostegno, e cade chi reggeva
  solo grazie ai vicini. `ApplyAuraMorale(0)` fa rientrare il prestito, e il `do...while`
  di `ApplyAuras` gestisce il crollo a catena senza codice nuovo.
- **Il Coro cura**: `ExecuteChant` chiama `ClearPanic()` su chi canta e sui sei vicini,
  accanto a `GainMorale`. ⚠ Nota numerica: in panico `MaxMorale` è quello base, quindi
  il `+1` può essere troncato e solo dopo `RefreshBoardState` il prestito risale.
  ✅ **Playtest 13/08/26: va bene così per ora.** 3 PA curano fino a sette unità mentre
  la carica ne costa 4 — la cura resta più economica dell'attacco, ed è accettato.
  ⚠ **La direzione decisa non è ritoccare il costo, è dividere il Coro in tipi**
  (Edoardo, 13/08/26): **coro di Morale** (l'attuale, cura), **coro antipanico**,
  **coro provocatorio** (attira le guardie). Un'unica azione che fa tre cose insieme è
  il motivo per cui è troppo economica: separandola, ogni coro paga il proprio effetto e
  il giocatore sceglie *quale* problema risolvere invece di risolverli tutti con un
  bottone. Il provocatorio in più è la prima azione del corteo che **manipola l'IA**
  invece di subirla, e collega il Coro alla Provocazione già in sospeso (GDD cap. 20) e
  alla priorità 4 del cap. 16. Non farlo prima di `ActionSO`: sono tre azioni nuove con
  costi propri, ed è esattamente il caso d'uso che rende maturo quel refactor.
- **Ordine obbligato in `PushResolution`**: cattura della cella d'urto → spinta →
  se ancora vivo, aggiorna la cella e applica il −1 → onda → `RefreshBoardState`.
  ⚠ **La cella si cattura PRIMA della spinta**, non dopo. La prima stesura la leggeva
  dopo (`impactCell = def.PositionCell`), e funzionava **solo perché `Disperse()` e
  `Arrest()` non azzerano `_positionCell`** — cioè si appoggiava alla voce 4 dei bug
  noti. Il giorno che quel bug si corregge, `impactCell` diventa `null`, l'onda sparisce
  e `ApplyPanicWave` va in `NullReferenceException` sul `Debug.Log`. Adesso la cella è
  catturata quando esiste di sicuro, e riaggiornata solo se il difensore è sopravvissuto.
  `ApplyPanicWave` ha comunque una guardia `origin == null` come rete.
- **Il −1 appartiene alla CARICA, non al panico.** Chi viene caricato due volte perde
  2 di Morale, esattamente come chi subisce due scontri. La regola "chi è già in panico
  non paga di nuovo" riguarda **l'onda**, ed è garantita dal fatto che solo il passo 0
  toglie Morale. Sono due cose diverse che è facile confondere.
- `MoraleLossCause.Panic` è **orfano**: il −1 usa `CauseFrom(atk)` e l'onda non toglie
  Morale a nessuno. Il commento nell'enum ("non ancora implementata") è falso.
- **Visualizzazione**: tremore laterale via DOTween su `_graphicsTransform` in **X**
  (`DOLocalMoveX`, yoyo, `Ease.Linear`), campo `_panicTween` **separato** da
  `_movementLoopTween` — altrimenti `StartBobLoop` lo ucciderebbe al primo movimento.
  L'asse X è libero: i movimenti scrivono su `_rootTransform`, il bob su
  `_graphicsTransform` in Y, il flip sulla scala. Sincronizzato da
  `UnitsRenderer.UpdateView`, che lo spegne anche nel ramo `!IsAlive` **prima** di
  `SetActive(false)`.
- ✅ **La riga nel pannello unità c'è dal 10/08/26**: `PANICKED — N turn(s)`, scritta da
  `UnitStatsPanelView.DescribeStatus`. Insieme alla tinta e al tremore, il panico adesso
  si vede in tre modi. ⚠ Attenzione: il campo `_statusText` va assegnato su **entrambi**
  i pannelli — vedi la sezione "Il pannello unità esiste in DUE copie".

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
- Catena tappata → si prova lo **sfogo laterale** (sotto), risalendo la colonna dal
  fondo. Se non ce l'ha nessuno, chi ha subito la carica esce di scena via
  `RemoveFromBoard(CauseFrom(pusher))` — arresto se l'ha spinto la polizia, dispersione
  altrimenti. **Esce il difensore, non l'ultimo della fila**: chi viene schiacciato
  contro la linea di polizia è chi viene preso.
- ⚠ **Non c'è più un ramo `Lose`**: dall'08/08 la carica non confronta Atk e Def, quindi
  la spinta va sempre nella stessa direzione e la catena si costruisce sempre fra i
  compagni di chi la subisce. Vedi la sezione dedicata più sotto.

## Sfogo laterale (AGGIUNTO 06/08, RIDISEGNATO 08/08/26)
`TryBuildPushChain` + `BuildMovesFromColumn` + `TryReleaseSideways` + `FindSideCell`.
`TryStepAside` **non esiste più**: la sua logica è dentro questi.

**Come funziona.** La catena si costruisce come prima, raccogliendo la `column` di unità
che la spinta comprime. Quando trova un tappo, invece di fallire subito si risale la
colonna **dal fondo verso il difensore** cercando il primo che abbia una cella laterale
libera. Quello scarta, **libera la sua cella**, e tutta la fila davanti a lui arretra di
uno; chi sta dietro di lui resta fermo. Solo se nessuno della colonna ha uno sfogo, chi
ha subito la carica esce di scena.

Lo sbandamento è quindi **la valvola di sfogo della compressione**, non un'alternativa
alla catena: la folla si comprime finché può, poi qualcuno in fondo sguscia di lato.

⚠ **La versione del 06/08 faceva scartare solo il difensore** e solo a catena fallita.
Cambiata l'08/08 su richiesta di Edoardo: adesso può scartare **chiunque** nella colonna.

- Le due celle candidate sono quelle delle **direzioni che affiancano** quella della
  spinta: `Directions[(i ± 1) % 6]`. Su esagoni due celle adiacenti condividono sempre e
  solo due vicini, e sono esattamente quelli.
  ⚠ **Dipende dal fatto che `HexCoordinates.Directions` sia in ordine ciclico**
  (E, NE, NW, W, SW, SE). Riordinare quell'array rompe lo sfogo in silenzio —
  `GetNeighbors` continuerebbe a funzionare, quindi movimento e pathfinding non se ne
  accorgerebbero. C'è un commento di avviso sopra la dichiarazione dell'array.
- Fra le due si sceglie quella con **meno alleati adiacenti**: la spinta disgrega.
  Deterministico di proposito: un domino casuale non è diagnosticabile.
- ⚠ **L'ordine dentro `moves` è la parte fragile.** `ApplyPushChain` applica
  dall'ultimo al primo perché `SetPosition` passa da `TryOccupy` e fallisce su cella
  ancora occupata. Quindi chi scarta dev'essere l'**ultimo** elemento della lista: si
  sposta per primo e libera la cella in tempo. Invertire i due `for` in
  `TryReleaseSideways` fa fallire la catena a metà con un `LogError`.
- ⚠ **`CountAdjacentAllies` ora è un'approssimazione.** Conta i vicini nello stato
  attuale, ma le unità davanti a chi scarta stanno per spostarsi. Esatto per chi sta
  dietro, ottimistico per chi sta davanti. Serve solo a scegliere fra due celle, quindi
  il danno massimo è una scelta subottimale — mai una posizione illegale. Con la versione
  del 06/08 era esatto perché si muoveva una sola unità.
  `if (other == unit) continue` resta necessario: al momento del conteggio l'unità non si
  è ancora spostata e confina con entrambe le candidate.
- Filtri identici a quelli della catena: bordo mappa, `!IsWalkable`, barricata, cella
  occupata (via `IsCellAvailable`) e **cella obiettivo** — altrimenti "l'obiettivo non si
  prende per spinta" varrebbe solo in una direzione.
- **Non esiste un domino laterale**: chi scarta si toglie dai piedi da solo, quelli
  dietro e di fianco non si muovono. Deciso, non emerso.
- ✅ **L'arresto per schiacciamento è raro, e va bene così** (playtest, 13/08/26).
  Prima bastava una cella bloccata; con la versione del 06/08 ne servivano tre; adesso
  serve che **tutta la colonna** sia tappata — `2N+1` celle bloccate per una colonna di
  N. Su campo aperto non succede praticamente mai: serve un corridoio stretto o una fila
  di seduti sui fianchi.
  **Il punto è che l'arresto non è il danno principale della spinta.** In playtest la
  spinta paga comunque: **smonta la Coesione e rallenta il corteo**, che deve spendere
  PA per ricompattarsi. Chiude un dubbio aperto il 05/08 — il domino sembrava ridondante
  col panico, e invece i due colpiscono cose diverse: il **panico** toglie aure e Morale,
  la **spinta** toglie legami e tempo. L'arresto è il caso limite, non il meccanismo.
- ⚠ **Tutto teletrasporta**: né la catena né lo sfogo hanno animazione.
- `ResolvePushOrRemove` ha in cima una **guardia di adiacenza** (`Distance != 1` →
  `LogError` e return). La direzione della spinta è la differenza fra le coordinate, che
  è una direzione valida solo se i due sono adiacenti. Senza la guardia,
  `TryBuildPushChain` costruirebbe una catena **a salti di due celle** senza dire niente.
- Il limite è lo **spazio attorno alla colonna**, non la lunghezza della fila.
- **I log dicono quale ramo è scattato**: `applied: N unit(s) moved`,
  `steps aside to (q,r), N unit(s) shift back`, `column of N blocked at (q,r): <motivo>`,
  `no way back and no way out`. Il motivo del tappo è nominato per esteso (bordo mappa,
  obiettivo, barricata, seduto, nemico): serve perché a schermo un tappo è invisibile.
- **Obiettivo = muro** (regola tematica scelta il 05/08/26: "il ministero non si prende
  per spinta"). La **cella obiettivo blocca anche se libera**: ci si cammina sopra, non
  ci si viene spinti.
  ⚠ **In playtest non è mai scattata, e il motivo è la MAPPA, non la regola** (13/08/26).
  Su `LVLTest` gli obiettivi sono incastrati fra altre celle obiettivo e celle non
  calpestabili, quindi non c'è mai nessuno *davanti* a un obiettivo che possa esservi
  spinto contro. **Non è quindi una regola validata: è una regola inerte.** Il timore del
  05/08 — "il corteo schierato davanti a un obiettivo si fa arrestare" — non è stato
  smentito, semplicemente non si è potuto presentare.
  ⚠ **Torna viva appena un livello mette un obiettivo in campo aperto**, e a quel punto
  nessuno si ricorderà perché. Chi disegna mappe deve sapere che una cella obiettivo
  raggiungibile da più lati è anche un muro per la spinta, quindi una trappola per chi le
  sta davanti. Da rivedere insieme all'occupazione temporanea del cap. 19.
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

## Obiettivi (IMPLEMENTATO 14/08/26) — GDD cap. 19
`ObjectiveSO` (dati) + `ObjectiveRuntime` (stato) + `HexGrid.BindObjectives` +
`LVLManager`. Sostituisce il vecchio punteggio: `_scoreToWin`, `_scoreForOccupation` e
`_currentScore` **non esistono più**.

- Un obiettivo è un **gruppo connesso di celle** dipinte come terreno obiettivo, raccolto
  per flood fill da **una** coordinata d'ancora dichiarata nell'SO. Si scrive una
  coordinata per obiettivo, la forma la dipinge il level designer.
- Si occupa accumulando **celle-turno**: ogni turno si somma il numero di celle
  dell'obiettivo occupate da spezzoni vivi. A `Required` è **rivendicato** e non paga più.

⚠ **`Required = Cells.Count + 1`, e il +1 non è una taratura** (aggiunto 20/08/26 dopo
playtest). In un turno non si può accumulare più di `Cells.Count` celle-turno — le celle
sono quelle. Quindi con `Required = Cells` bastava **coprire l'edificio** per rivendicarlo a
fine turno, **senza che la polizia avesse mai un turno per reagire**: il livello si vinceva
al turno 5 senza un solo scontro. Col +1 servono **sempre almeno due turni**, e il turno in
mezzo pesa perché l'accumulo si azzera se molli la presa. Occupare smette di essere
"toccare" e diventa "tenere".
⚠ È facilissimo scambiarlo per un off-by-one: c'è un commento lungo sopra la proprietà.
⚠ Gli obiettivi da 1 cella passano da 1 a 2 celle-turno — voluto.

⚠ **`_requiresSimultaneous` NON è un'eccezione alla finestra di due turni** (deciso 20/08/26,
dopo che una revisione esterna aveva trovato che lo era). Il ramo simultaneo faceva
`_progress = Required` e rivendicava **nello stesso tick**, cioè la garanzia valeva per tutti
tranne lui. Adesso il flag è **solo un cancello**: se non tieni tutte le celle l'accumulo si
azzera, altrimenti accumuli come tutti (`_progress += occupied`). Due turni, sempre.
*Regola generale che ne esce: una garanzia con un'eccezione non è una garanzia, è una
coincidenza — e nessuno si ricorda le eccezioni al momento di costruirci sopra.*
- ⚠ **Se in un turno non c'è nessuno sopra, l'accumulo si AZZERA.** L'obiettivo è una
  finestra da difendere, non un lavoro da rosicchiare.
- `_requiresSimultaneous` sull'SO ripristina il profilo "ti obbliga a spezzarti": servono
  tutte le celle nello stesso turno.
- **La vittoria è rivendicare l'obiettivo dichiarato** (`LVLManager._declaredObjective`).
  Oggi lo decide il livello, domani il volantino (GDD 20.3).
- Il `Tick()` di ogni obiettivo gira in `LVLManager.OnEventRaised`, cioè a fine turno del
  giocatore — lo stesso momento della Coesione.

⚠ **`HexCell.IsObjective` e `HexTypeSO.IsObjectiveGround` sono DUE COSE DIVERSE**, ed è la
distinzione più facile da sbagliare del sistema:
- `cell.IsObjective` = *appartiene a un obiettivo dichiarato*. **Lo leggono le regole di
  gioco**: muro per la spinta, divieto di barricata, sfogo laterale.
- `type.IsObjectiveGround` = *è dipinta come terreno obiettivo*. **Lo legge solo il flood
  fill**, per raccogliere la forma.
Una cella dipinta ma non dichiarata è verde e basta: non fa muro, non dà punti.
⚠ Il campo si chiamava `_isObjective` e ha `[FormerlySerializedAs]`: **non toglierlo**
finché tutti gli asset `HexTypeSO` non sono stati risalvati, o il flag si azzera in
silenzio su una mappa dipinta a mano.

**Stato della mappa al 16/08/26**: 10 obiettivi su `LVLTest`, 35 celle, **zero orfane**.
Le taglie vanno da 1 a 6 celle. Chiude il sospetto che le 35 celle fossero una colonna
dipinta per errore: erano dieci edifici.

## Punti di ritrovo e spawn del corteo (IMPLEMENTATO 16/08/26) — GDD cap. 20
`MeetingPointSO` + `MeetingPointRuntime` + `HexGrid.BindMeetingPoints` +
`LVLManager.SpawnRoster`.

- **Stesso schema degli obiettivi**: pennello (`HexTypeSO.IsMeetingGround`) + ancora +
  flood fill. `FloodGroup(start, predicato)` è generico e lo usano entrambi.
- ⚠ **La capienza del ritrovo È il limite del corteo**: quante celle è grande la piazza.
  Non è un parametro da tarare, è quello che hai dipinto. Su `LVLTest` le tre piazze
  valgono 7, 14 e 16.
- Il corteo nasce sulle celle in **ordine di flood fill dall'ancora**, quindi parte
  compatto — Coesione alta e aure attive. È l'ancora a decidere il centro dello schieramento.
- Celle occupate (es. un poliziotto in piazza) vengono **saltate**, non consumate: un posto
  bloccato non deve costare un'unità del corteo.

## Presidio, guinzaglio e allarme (IMPLEMENTATO 18/08/26) — GDD cap. 8
`PoliceRuntime.AssignGuard` + `LVLManager.AssignGarrisons` / `RaiseAlarmAround` /
`CheckObjectiveIntrusion` + `PoliceAI` a tre passate. **La polizia non insegue più: presidia.**

- Ogni poliziotto riceve in `LVLManager.Start` (dopo lo spawn) un **obiettivo da difendere**,
  delle **regole d'ingaggio** e un **raggio di guinzaglio**. La fonte è a cascata: campo
  sull'`UnitsSetup` in scena se valorizzato, altrimenti il default del livello.
  Se l'obiettivo dichiarato sull'`UnitsSetup` **non sta su questa mappa** è un `LogError`,
  non un ripiego silenzioso: è un errore di dato del level designer.
- Senza obiettivo dichiarato si ripiega su `NearestObjective` — **il più vicino per cella,
  non per ancora**: un obiettivo grande va difeso dal lato da cui gli sei accanto.
- **Il guinzaglio si misura dall'obiettivo, non dalla cella di partenza.** `IsWithinLeash`
  restituisce `true` in tre casi che sono altrettante vie di fuga dal presidio:
  regole `Sweep`, nessun obiettivo assegnato, **oppure unità in allarme**.
- `PoliceAI` è a **tre passate**, in quest'ordine: (0) se sei fuori guinzaglio torna al posto;
  (1) agisci — saltata in `Containment` se non sei in allarme; (2) avvicinati restando dentro
  il guinzaglio. `MoveTowards` **tronca il percorso alla prima cella fuori raggio**: il
  guinzaglio è un vincolo sul cammino, non solo sulla destinazione.

⚠ **`RaiseAlarmAround` è l'unico modo per staccare la polizia dal posto.** Senza chiamanti
il presidio è una statua e il corteo gli cammina accanto. Le vie sono **due famiglie**:

**1. Aggressione a un poliziotto** — passa tutta da `TurnManager.ReportAggression(victim,
aggressor, origin = null)`, chiamata da `ExecuteSkirmish`, `ExecuteThrow` e `PushResolution`.
🔴 **Fino al 20/08/26 l'allarme era scritto a mano nel solo `ExecuteSkirmish`**, quindi
**lanciare un sanpietrino o caricare un poliziotto non svegliava nessuno**: due modi di
aggredire su tre erano muti, senza produrre nessun errore. Trovato da Edoardo in playtest.
⚠ Tre chiamanti ma **una decisione sola**: chi aggiunge un'azione ostile chiama
`ReportAggression`, non riscrive la regola. È lo stesso schema di `CauseFrom`.
⚠ Il parametro `origin` esiste perché la **carica** deve passare la cella dell'**urto**,
catturata prima della spinta: dopo, la vittima si è spostata o è uscita di scena.

**2. Intrusione in un obiettivo** — `LVLManager.CheckObjectiveIntrusion`, chiamata dalla
callback di `TurnManager.ExecuteMovement` **e da `PushResolution`**. Entrare in un obiettivo
non rivendicato sveglia il presidio nel raggio; su un obiettivo **già rivendicato** non
scatta — l'ultimo obiettivo del livello sarebbe gratis, ma quello preso non deve suonare in
eterno.
🔴 La chiamata in `PushResolution` è del 20/08/26: la carica sposta l'attaccante, e finché il
controllo viveva solo in `ExecuteMovement` **entrare caricando era un modo legale di infilarsi
in un edificio presidiato senza svegliare nessuno**. `HasChargeRoom` valida la destinazione con
`IsCellAvailable`, che non guarda `IsObjective`.

⚠ **Regola che ne esce, e vale oltre l'allarme**: agganciare una conseguenza agli *esecutori*
invece che al *fatto* garantisce che prima o poi qualcuno se ne dimentichi. Sono tre casi in
un giorno — l'allarme, `CanPerformAction` che vive solo in `InputHandler`, e l'intrusione da
carica. Quando una regola vale "ogni volta che succede X", il posto giusto è dove X è nominato.

⚠ **L'allarme decade da solo**, `TickAlarm()` in `ExecutePoliceTurn` — cioè dove si
decrementa il panico della polizia e si ricaricano i suoi PA. `RaiseAlarm` usa `Mathf.Max`,
quindi due incidenti ravvicinati non si sommano ma **rinnovano** la durata: è lo stesso
schema del panico e per la stessa ragione (un allarme debole non deve *curare* chi è già
in massima allerta).

⚠ **Conseguenza di design da tenere presente**: con l'allarme all'ingresso, **gli obiettivi
secondari diventano lo strumento di diversivo** — entri in uno lontano, svegli quel
presidio, e prendi il dichiarato mentre sono impegnati. Non serve una Zona Rossa né un coro
provocatorio per avere una distrazione: c'è già, ed è emersa invece che essere progettata.

🔴 **Due bug chiusi il 20/08/26 dalla revisione esterna, entrambi nel rientro al presidio.**
- **`MoveTowards` impediva il rientro invece di imporlo**: troncava il percorso al primo passo
  fuori guinzaglio, ma chi è *già* fuori non poteva fare nemmeno il primo passo verso casa.
  ⚠ **La prima correzione era a metà** e va raccontata perché la lezione vale: ammetteva i
  passi che *avvicinavano* al presidio, e questo bastava in campo aperto ma non con un muro in
  mezzo — una deviazione attorno a un edificio ti allontana temporaneamente, quindi il blocco
  permanente rientrava dalla porta di fianco. La formulazione giusta è più semplice:
  **il guinzaglio serve a impedirti di allontanarti, non a dettarti la strada di casa.** Se sei
  già fuori raggio (`returningToPost`) il percorso non è vincolato affatto; il vincolo per passo
  vale solo quando sei dentro.
- **`NearestPostCell` sceglieva la cella per pura distanza esagonale**, e `PathFinder.FindPath`
  scarta ogni cella non disponibile **destinazione compresa**: bastava che quella cella fosse
  occupata — tipicamente da un collega — perché il rientro fallisse per sempre. Sostituita da
  `FindReachablePostCell`, che prova le celle in ordine di distanza e restituisce la prima
  **raggiungibile**, o `null`.
  ⚠ Il caso in cui succedeva *sempre*: **un obiettivo da 3 celle con 4 poliziotti assegnati**
  (è la situazione prodotta dal richiamo del volantino su `LVLTest`).
  ⚠ Conseguenza che rendeva il bug peggiore di quanto sembri: la passata 0 fallita fa `break`,
  quindi salta anche le passate 1 e 2 — il poliziotto non tornava a casa **e non attaccava
  nemmeno chi aveva accanto**.

🔴 **Hard lock chiuso lo stesso giorno**: `TurnManager` validava `_lvlManager` e `_pathFinder`
ma **non `_policeAI`**, e `ExecutePoliceTurn` lo dereferenziava senza guardia. Con il campo non
assegnato, `EndTurn` metteva `_waitingForPolice = true`, la `NullReferenceException` uccideva la
coroutine prima della riga che lo rimette a `false`, e l'input restava bloccato per sempre.
Ora c'è un `LogError` in `Start` (**senza `return`**, o non si alzerebbe `_startPlayerTurnEvent`)
e una guardia in `ExecutePoliceTurn` che salta il turno della polizia invece di piantarlo.

⚠ **Playtestato il 20/08/26 e funzionante** (i richiamati camminano verso il nuovo presidio,
il lancio sveglia il presidio, l'inseguimento dura tre turni). Restano da tarare: nessuno ha
verificato che i tre numeri (`_leashRadius` 4, `_alarmRadius` 4, `_alarmDuration` 3) diano
un avversario giocabile. Sono manopole, e sono su `LVLManager` proprio per poterle girare.

### `UnitsSetup` non deduce più la cella da solo
`Initialize(HexGrid grid, HexCell startCell = null)`:
- `startCell == null` → la cella si deduce da `WorldToGrid(transform.position)`. È il caso
  delle unità piazzate a mano in scena, **oggi la polizia**.
- `startCell` valorizzata → l'unità nasce lì. È lo spawn a runtime del corteo.

⚠ **Il campo serializzato `_grid` è stato tolto**: un prefab non può tenere un riferimento
a un oggetto di scena. La griglia arriva da fuori, da `LVLManager`.

⚠ **Due fasi di spawn in `LVLManager.Start`**: `SpawnSceneUnits()` (tutti gli `UnitsSetup`
già in scena) e poi `SpawnRoster()` (istanzia i prefab del roster sul ritrovo). L'ordine
conta: le unità di scena occupano le loro celle prima che il roster cerchi posto.
`RegisterUnit` è il punto unico dove si aggiorna liste, renderer e componenti.

## Highlight (OrderPreviewRenderer)
- Alla selezione di uno spezzone: una sola BFS via `TacticalQuery.GetReachable`
  produce `visited` (celle raggiungibili entro budget PA), passato sia a
  HighlightReachable (celle blu) sia a HighlightAttackable.
- Celle raggiungibili: blu. Scontro disponibile: rosso. Carica: giallo.
  Muovi+attacca: rosso (stesso dello scontro — vedi nota).

## Animazione e feedback visivo (VERIFICATO 10/08/26)

### Gerarchia dell'unità — leggere prima di toccare qualunque cosa grafica
Un'unità in scena è **due livelli**: la radice (es. `PoliceBasic`) e un figlio **`Logic`**
che porta `UnitsSetup`, `UnitMovement` e `UnitStatusView`. Il grafico sta in un altro
figlio (es. `PoliceGraphics`).

⚠ Un componente nuovo va messo sul figlio `Logic` di **tutti e sei** i prefab unità
(`AnarkyUnit`, `BlackBlock`, `PacifistUnit`, `StudentsUnit`, `WorkersUnit`, `PoliceBasic`),
e va verificato **sul prefab**, non sull'istanza in scena: un componente aggiunto solo
all'istanza funziona in `LVLTest` e sparisce nel livello successivo. Verifica rapida:
cercare il guid del `.cs.meta` dentro i sei `.prefab` — devono essere sei occorrenze.

⚠ Conseguenza: `UnitsRenderer._unitsDict` mappa l'unità sul GameObject **`Logic`**, non
sulla radice — perché `LVLManager` registra `setup.gameObject`. Ecco perché `UpdateView`
usa `go.transform.root.position` e **non** `go.transform.position`: deve muovere tutta
l'unità, non solo il nodo logico. Due revisori esterni hanno proposto di "correggerlo" in
`transform.position`: **avrebbe rotto il movimento.** Non farlo.

### Chi scrive dove
| Canale | Chi lo usa |
|---|---|
| `_rootTransform.position` | movimenti, scontro, carica (`UnitMovement`) |
| `_graphicsTransform.localPosition.y` | bob durante il movimento (`UnitMovement`) |
| `_graphicsTransform.localPosition.x` | tremore da panico (`UnitStatusView`) |
| `_graphicsTransform.localScale` | flip dello sprite (`UnitMovement`) |
| `SpriteRenderer.color` | tinta di stato e lampo (`UnitStatusView`) |

Sono canali **distinti apposta**: due sistemi possono animare la stessa unità senza
pestarsi i piedi, ma solo finché ognuno resta sul suo. È la prima cosa che si perde di
vista e produce bug incomprensibili.

### `UnitMovement`
Solo spostamenti e combattimento: `MoveAlongPath` (Lerp smoothstep cella per cella + bob),
`PlaySkirmish` (windup → colpo → recoil, con `onImpact` fra i primi due), `PlayHitReaction`,
`PlayCharge` (windup DOTween + rincorsa Lerp), flip.

### `UnitStatusView` (NUOVO 10/08/26) — `Units/Visualization/`
Mostra la **condizione** dell'unità e nient'altro. Il capitolo 17 del GDD lo prescriveva
da agosto; prima il tremore stava in `UnitMovement` come ripiego.

- `Refresh(panicked, seated)` — chiamato da `UnitsRenderer.UpdateView`. Tinta + tremore.
  Il panico vince sul seduto.
- `Flash()` — lampo rosso da danno.
- `Clear()` — da chiamare **prima** di disattivare il GameObject.
- ⚠ **È l'unico proprietario di `SpriteRenderer.color`.** Il lampo sfuma verso
  `_currentTint`, **non** verso il bianco: un'unità in panico deve tornare grigia dopo il
  colpo, non normale. E `ApplyTint` esce se un lampo è in corso, altrimenti lo taglia a metà.
- La tinta **moltiplica** i colori di base invece di sostituirli: conserva la tinta
  originale dello sprite, e con `Color.white` è neutra.
- ⚠ `CacheTintables` **esclude i SpriteRenderer sul layer "Outline"**: `SelectionOutline`
  ne crea di duplicati e il suo `Initialize` gira prima del nostro. Senza il filtro,
  tingendo l'unità tingeresti il suo contorno di selezione.
- ⚠ `sr.color` **moltiplica, non desatura**. Il "grigio del panico" è un grigio freddo che
  legge come spento; per una desaturazione vera servirebbe un parametro nello shader.

### Il lampo da danno sta all'IMPATTO, non in `LoseMorale`
Regola stabilita il 10/08/26 dopo un tentativo sbagliato.

Il primo aggancio era un `System.Action Damaged` alzato da `LoseMorale` — punto unico,
elegante, **e visivamente sbagliato**: per la regola "logica prima, animazione dopo" il
Morale scende al clic, mentre l'attaccante sta ancora caricando il colpo. Il lampo partiva
prima dell'animazione.

**Non esiste un istante unico di "colpo": ogni azione ha il suo.**
- **Scontro** → dentro `onImpact` di `PlaySkirmish`, insieme a `PlayHitReaction`.
  `ExecuteSkirmish` raccoglie chi è stato colpito in una lista `hit` nello stesso `switch`
  che applica il Morale, e `onImpact` la legge: **una decisione, una lettura**.
- **Lancio** → nell'`OnComplete` del `DOJump` in `ThrowObjectVFX`, quando l'oggetto atterra.
- **Carica** → in `PushResolution`, che gira già dentro la callback dell'animazione.

⚠ **Perché qui la duplicazione è legittima e altrove no.** Unificare la legalità serviva
perché era *una decisione ripetuta*, e due copie divergono in modo pericoloso. Il momento
d'impatto invece è **informazione diversa per ogni azione** — lo scontro ha un windup, il
lancio un tempo di volo, la spinta niente. Non è duplicazione, è specificità.

⚠ **Caso analogo ancora aperto**: `ExecuteThrow` chiama `UpdateView(target)` subito, quindi
se il lancio uccide, il bersaglio sparisce **mentre l'oggetto è ancora in volo**. Stessa
famiglia dell'animazione dell'arresto: qualcosa esce di scena troppo presto rispetto a
quello che si vede.

## Ricompilare durante il Play azzera i campi non serializzabili (20/08/26)
Sintomo: raffica di `NullReferenceException` in `CameraManager.OnEnable`,
`InputHandler.OnEnable`/`OnDisable`, ripetute in cicli enable/disable. **Non è un bug del
gioco**: succede quando Visual Studio ricompila mentre l'Editor è in Play.

- Al **domain reload** Unity conserva lo stato serializzabile, chiama di nuovo `OnEnable`,
  ma **non richiama `Awake`**. Tutto ciò che veniva costruito in `Awake` e non è
  serializzabile torna `null`.
- Nel progetto è `_inputSystem` (`new InputSystem_Actions()`), che è una classe C# pura e
  quindi non sopravvive al reload.

⚠ **Il pattern `_isValid` NON protegge da questo, ed è la parte istruttiva.** In
`InputHandler` la guardia `if (!_isValid) return;` c'è e non è servita: `_isValid` è un
`bool`, quindi **sopravvive** al reload restando `true`, mentre `_inputSystem` — che è ciò
che la guardia dovrebbe proteggere — sparisce. **La guardia sopravvive e l'oggetto guardato
no.** Regola generale: un flag di validità è valido solo finché tutto ciò che attesta ha lo
stesso ciclo di vita del flag.

Cura: costruire pigramente invece che solo in `Awake` — `_inputSystem ??= new
InputSystem_Actions();` in cima a `OnEnable`, in `CameraManager` e `InputHandler`.
(Rimedio immediato senza toccare codice: uscire dal Play e rientrare.)

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
`D:\GDDRIOT\FIXLIST_2026-08-06.md`.

**STATO AL 06/08/26 SERA: tutti i bug attivi sono chiusi tranne uno** (la divergenza
highlight/esecuzione su SitStand, Throw e Barricade, vedi sotto). Le voci restano qui
con la diagnosi, perché la spiegazione del *perché* succedeva vale più della correzione.

- **✅ RISOLTO — il giocatore poteva agire durante il turno della polizia.**
  Fix: predicato unico `InputHandler.CanAcceptPlayerInput` (che include
  `!_turnManager.IsPoliceTurn` e i null-check sui riferimenti) in cima ai nove punti
  d'ingresso. `TryEndTurn` è lasciata fuori di proposito: `EndTurn` ha già la sua guardia.
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

- **✅ RISOLTO — un'unità poteva finire SOPRA una barricata.**
  Fix a due livelli: `if (_barricade != null) return false;` in `TryOccupy`, e
  `MoveCoroutine` che ora **usa il valore di ritorno di `SetPosition`** (`break`, non
  `yield break`, così `onComplete` viene comunque invocato) e riporta la grafica sulla
  cella logica. Il secondo è quello che conta: la forma generale del problema è
  "percorso calcolato prima, applicato dopo, mai rivalidato".
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

- **✅ RISOLTO — `EndTurn` proseguiva dopo il game over.** `_endPlayerTurnEvent.Raise()` è
  **sincrono**: dentro, `LVLManager.OnEventRaised` può decretare fine partita, alzare
  win/lose e fare `_turnManager.enabled = false`. Al ritorno del listener, `EndTurn`
  **continua**: ricarica i PA della polizia e chiama `StartCoroutine(ExecutePoliceTurn())`,
  che a fine coroutine rialza `_startPlayerTurnEvent` su una partita conclusa.
  `enabled = false` non interrompe un metodo già in esecuzione — questo è certo e vale
  come regola generale. Fix: `if (!_lvlManager.IsGameActive) { _waitingForPolice = false; return; }`
  subito dopo il `Raise`, e la stessa guardia in `ExecutePoliceTurn`.

- **✅ RISOLTO 08/08/26 — divergenza highlight/esecuzione su SitStand, Throw e Barricade.**
  Era l'ultimo bug attivo. Fix in quattro passi: costanti e predicati in `TacticalQuery`
  (`GetSitStandCost`, `CanThrow`, `CanPlaceBarricade`) → esecutori che li chiamano →
  `GetValidTargets` che riceve **unità e oggetto** invece di coordinata e budget →
  `OrderPreviewRenderer` che impara a conoscere l'oggetto selezionato.
  Chiusa gratis anche la voce 13 dell'arretrato (barricata sulle celle obiettivo), e
  `ExecuteThrow` adesso verifica la gittata invece di fidarsi del chiamante.
  ⚠ **Coda rimasta**: `TurnManager.CanCharge` è l'unico predicato ancora fuori da
  `TacticalQuery`, quindi la carica ha ancora due luoghi di verifica. Spostarla tocca
  `PoliceAI`. Mezz'ora, non urgente.
  Testo originale della diagnosi:
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

- **✅ RISOLTO — `SelectionOutline` si iscriveva a quattro eventi senza guardie** (né `_isValid` né
  null-check). È l'unico posto del progetto senza rete, e sta sui **prefab delle unità**:
  un campo non assegnato si moltiplica per ogni unità che spawna, e l'eccezione arriva
  dentro il `Start` di `LVLManager`, mentre sta costruendo il livello.
  (`CameraManager` non usa `_isValid` ma protegge ogni `Subscribe` con `if (event != null)`:
  quello va bene.)

- **✅ RISOLTO — `UnitsSetup.Initialize`: la guardia stava DOPO l'uso.** Nel `foreach` sull'inventario
  iniziale, `AddItem(s.item, s.quantity)` viene chiamato **prima** del `if (s.item == null
  || ...) continue`, che quindi non salta più niente. Una riga vuota nell'array inserisce
  uno slot con `Item = null`, che poi `InventorySlotUI.SetItem` dereferenzia.

- **✅ RISOLTO — nessun `WaitUntil` aveva un fail-safe.** Ora tutti e tre hanno un timeout
  a 5 secondi più un `LogWarning`. ⚠ **In `ExecuteCharge` il timeout da solo non bastava**:
  `PushResolution` sta nella callback, quindi allo scadere la carica risultava pagata e mai
  risolta. Risolto con una funzione locale `ResolveOnce()` protetta da un flag `resolved`,
  chiamata sia dalla callback sia dal ramo di timeout. **Il flag non è opzionale**: il
  timeout non uccide l'animazione, quindi una callback in ritardo farebbe girare
  `PushResolution` due volte — cioè una spinta doppia o un `Vacate()` su una cella ormai
  di un altro, che è il bug della resurrezione del 05/08.
  Testo originale: `ExecuteCharge`, `ExecuteSkirmish` e
  `PoliceAI` aspettano un flag alzato da una callback di animazione. Se la callback non
  arriva (tween ucciso, GameObject disattivato), lato giocatore `_isExecutingAction`
  resta `true` e input e Fine turno si bloccano per sempre; lato IA `_waitingForPolice`
  resta `true` e il turno non torna mai. **Nessun errore, nessun log: il gioco si pianta.**
  In `BootManager` ogni attesa ha un timeout proprio per questo; qui no.

- ~~**I costruttori Runtime ignorano `TryOccupy`, e `Vacate()` non controlla chi libera.**~~
  — **RISOLTO 16/08/26**, chiuso insieme allo spawn a runtime perché la lista di celle di
  partenza lo rendeva raggiungibile per errore di dato invece che di trascinamento.
  Diagnosi originale: due `UnitsSetup` sulla stessa coordinata → la seconda unità esiste in
  lista e in scena ma la cella indica la prima; quando la seconda si muove, il suo
  `Vacate()` **cancella dalla griglia la prima**.
  Fix a due livelli: `HexCell.Vacate(AbstractUnitsRunTime unit)` libera **solo se
  `_occupiedBy == unit`** (i tre chiamanti in `AbstractUnitsRunTime` passano `this`), e
  `UnitsSetup.Initialize` verifica `TacticalQuery.IsCellAvailable` **prima** di costruire,
  restituendo `null` con un `LogError`.
  ⚠ **I costruttori Runtime continuano a buttare il risultato di `TryOccupy`** — la
  verifica sta a monte. Chi scriverà un altro punto di creazione di unità deve rifare il
  controllo lì: il costruttore non può fallire.

- **✅ RISOLTO — `LVLManager.OnEnable` leggeva la griglia prima che `HexGrid.Awake` l'avesse
  generata.** `RefreshObjectiveCells()` è ora in `Start`. Log di conferma in gioco:
  `[LVL] Found 35 objective cells in the map`.
  ⚠ **35 celle obiettivo su una mappa 51×35 sono probabilmente troppe** — 35 è esattamente
  l'altezza, sospetto di una colonna intera dipinta `ObjectiveSO`. Con `_scoreToWin` a 30 e
  `_scoreForOccupation` a 10 si vince con tre unità su obiettivo per un turno. E siccome le
  celle obiettivo **fanno muro per la spinta**, un'area obiettivo larga rende gli arresti
  molto più frequenti del previsto. Da guardare in scena.
  Diagnosi originale:
  Unity garantisce `Awake` prima di `OnEnable` **sullo stesso componente**, non l'ordine
  incrociato fra GameObject. Se perde la corsa, `_objectiveCells` resta vuota: il punteggio
  non sale mai e si perde ogni livello per scadenza turni. Il log
  `Trovate N celle obiettivo nella mappa.` lo dice — se N è 0, è questo.
  Fix: spostare `RefreshObjectiveCells()` in `Start`.

- **✅ RISOLTO 08/08/26 — la conversione coordinate era sparsa e non uniforme.**
  `HexGrid` espone ora `GridToWorld(HexCoordinates)` e `WorldToGrid(Vector3)`, e **tutte
  e diciassette** le conversioni del progetto passano di lì. Regola: `transform.position`
  si somma in **un posto solo**, dentro `GridToWorld`. Verifica: `ToWorldPosition` e
  `FromWorldPosition` devono comparire solo in `HexCoordinates.cs` e `HexGrid.cs`.
  Diagnosi originale, che spiega perché valeva la pena:
  `UnitsRenderer.UpdateView` usava `Coordinates.ToWorldPosition(cellSize)` **senza**
  `_grid.transform.position`; `UnitsSetup` e `InputHandler` passavano posizioni mondo a
  `FromWorldPosition` senza sottrarre l'offset; `TurnManager`, `UnitMovement`,
  `ThrowObjectVFX` e `HexGridRenderer` invece lo sommavano.
  Invisibile perché `MapManager` è a `(0,0,0)`. Il giorno che si fosse traslata la griglia
  si sarebbero rotte **tre cose diverse insieme** — clic, spawn e `UpdateView` — e
  sarebbero sembrate causate dallo spostamento invece che da tre difetti preesistenti.

- **La regola `IsAlive` è già violata in quattro punti**: `TacticalQuery.GetAuraBonus`,
  `TurnManager.ExecuteChant`, `OrderPreviewRenderer.OnActionSelected` e `HighlightChantArea`
  confrontano con `UnitsStatus.Alive`. Con gli stati attuali il comportamento è identico,
  ma resta la regola. ⚠ **Correzione del 06/08/26**: una stesura precedente diceva "il panico
  è il prossimo stato non-vivo in arrivo". **Falso**: un'unità in panico è viva, si muove e
  agisce. Il panico è un campo a parte (`_panicTurnsLeft`), non un valore di `UnitsStatus`.
  La regola `IsAlive` resta valida per gli stati che verranno davvero (immobilizzato,
  ferito), non per il panico.

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

- ~~**`BootManager`: la PRIMA attesa non ha fail-safe.**~~ — **RISOLTO 16/08/26.** Il
  `WaitUntil(frame >= 0)` ha ora un timeout di 3 secondi con `LogWarning`. Diagnosi
  originale: precedeva la costruzione del timer di sicurezza, quindi una clip che non
  produceva un frame lasciava la bootscene sul nero senza mai arrivare al timeout su
  `clipLength + 2`.
  ⚠ **Adesso tutti e cinque i `WaitUntil` del progetto hanno una via d'uscita** — i due
  di `BootManager`, i due di `TurnManager` (carica e scontro) e quello di `PoliceAI`.
  Verificato con un grep il 16/08/26: se ne aggiungi uno, aggiungi anche il timeout.

- **`PushResult.IsResolved == false` confonde due casi opposti** (22/08/26). Copre sia
  "input non valido o unità non adiacenti" — dove non è successo niente — sia
  "`ApplyPushChain` fallito a metà catena", dove **la plancia è corrotta**. Il `LogError` in
  `PushResolution` dice *"invalid units, positions or adjacency"*, che per il secondo caso è
  falso; e il `return` che segue salta morale, panico, allarme, intrusione e
  `RefreshBoardState` su una plancia già mossa a metà. Serve un `Reason` nel `PushResult`.
  ⚠ Non è un bug attivo: il fallimento a metà catena non è producibile per i motivi sotto.
- **`ApplyPushChain` non è transazionale** (segnalato da entrambe le revisioni del
  16/08/26). Se un `SetPosition` fallisce a metà catena, le unità già spostate restano
  nelle celle nuove e le successive no: la griglia resta in uno stato parziale.
  **Non è un bug attivo**: costruzione e applicazione sono sincrone, non c'è `yield` in
  mezzo, nessun evento viene alzato fra le due, e lo spawn adesso difende l'invariante di
  occupazione. Nessun percorso normale produce quel fallimento.
  Il `LogError` è la sentinella: **se scatta, la plancia è corrotta**. Non aggiungere un
  rollback per un caso che non si presenta — costruire una transazione qui costerebbe più
  del problema.

- **Rientranza di `WinLevel` dentro il ciclo degli obiettivi** (segnalato 16/08/26).
  `LVLManager.OnEventRaised` itera su `_map.Objectives` chiamando `Tick()`, e se l'obiettivo
  dichiarato viene rivendicato alza `_winEvent` **in mezzo al `foreach`** — quindi un
  listener di vittoria vede gli obiettivi successivi non ancora aggiornati, e al ritorno il
  ciclo continua a modificarli. Oggi nessun listener se ne accorge. Diventerà un bug il
  giorno che lo schermo di fine livello calcolerà ricompense in base agli obiettivi
  secondari rivendicati: il risultato dipenderebbe dall'**ordine nella lista**.

- **Il timeout del movimento IA non annulla il movimento** (segnalato 16/08/26).
  `PoliceAI` aspetta 5 secondi e poi prosegue, ma non interrompe `MoveCoroutine`: se il
  timeout scatta davvero, l'IA continua mentre il movimento precedente è ancora in corso.
  Con i tempi normali non accade. È il fail-safe che sblocca senza rimettere ordine —
  stessa famiglia del timeout dello scontro, che invece è stato chiuso.

- **`TurnManager.CanCharge` è l'unico predicato di legalità rimasto fuori da
  `TacticalQuery`** (confermato da entrambe le revisioni del 16/08/26). È una query pura
  che vive nell'esecutore. Coda del refactor B3/B7, non urgente: spostarla tocca `PoliceAI`.

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
  ⚠ **Anche la seconda metà è RISOLTA il 06/08/26** col pattern `_isValid`, ma la
  vecchia diagnosi era SBAGLIATA e vale la pena sapere perché.
  Diceva: "l'AudioManager muore al cambio scena lasciando iscrizioni pendenti".
  **Falso**: quando Unity distrugge un GameObject allo scarico della scena chiama
  `OnDisable` prima di `OnDestroy`, quindi la disiscrizione avviene. Nessun leak.
  Il problema vero era più concreto: `Awake` e `OnEnable` sono **messaggi indipendenti**
  — un `return` dentro `Awake` non impedisce a `OnEnable` di girare. Quindi un
  AudioManager senza mixer si iscriveva lo stesso a `SceneManager.sceneLoaded`, e a
  ogni cambio scena `LoadAudioSettings()` andava in `NullReferenceException` su un
  mixer nullo — eccezione che arriva in una scena DIVERSA da quella dove è stato
  loggato il warning. In più `_playMusicEvent` non era controllato da nessuna guardia:
  se vuoto, `OnEnable` esplodeva sulla prima riga (ora è nel check).
  **Regola generale che resta**: un componente che non è in condizione di funzionare
  deve fallire *in modo chiuso* — non iscriversi a niente — non funzionare a metà.
  *Nota di metodo: questa voce l'ho ripetuta due volte prima di verificarla. Terza
  occorrenza dello stesso errore in una giornata.*
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
- **File non salvati in UTF-8 — il problema RIENTRA, non si chiude una volta sola.**
  I cinque censiti il 06/08 (`TacticalQuery.cs`, `CameraManager.cs`,
  `PlayFromAnyScene.cs`, `InventoryView.cs`, `UnitsSetup.cs`) sono stati sistemati.
  Poi è tornato su `PathFinter.cs` in sess.32, e **al 13/08/26 l'unico file non-UTF8 di
  tutto `Assets/Script` è `UnitsRenderer.cs`** (verificato decodificando ogni `.cs`).
  Sono byte accentati dentro commenti: non rompono niente — Unity ricade sulla codepage
  di sistema — ma nel flusso a due macchine basta che un editor ne risalvi uno perché
  git segni righe che nessuno ha toccato.
  ⚠ **La causa è nota e la contromisura non è stata applicata a tappeto**: Visual Studio
  ricade sulla codepage di sistema sui file UTF-8 **senza BOM**. Finché esistono file
  senza firma, ogni commento con un accento può riaprire la voce. Correggere il singolo
  file è un cerotto; la cura è **salvare col BOM** tutto ciò che si tocca.

  ✅ **CHIUSA il 18/08/26, e stavolta alla radice.** Due interventi insieme:
  1. **BOM su tutti e 74 i `.cs`.** Verificato dopo: zero file non-UTF8, zero file senza
     firma, graffe ancora bilanciate. ⚠ Nota importante sul rischio: **nessun file ha
     avuto bisogno della conversione da Windows-1252** — tutti e 74 decodificavano già
     come UTF-8 valido, quindi l'operazione ha solo *aggiunto tre byte* e non ha toccato
     un solo carattere. Il caso pericoloso (testo reinterpretato male) non si è presentato.
  2. **`.editorconfig` nella radice del repo** con `charset = utf-8-bom` per `*.cs`.
     È questo che chiude la voce per davvero: il BOM fa sì che VS *indovini giusto*,
     l'`.editorconfig` fa sì che **non debba indovinare**, e un file nuovo nasce firmato.
  ⚠ `end_of_line = crlf` nell'`.editorconfig` è coerente con `.gitattributes`
  (`* text=auto`, quindi LF nel repo e CRLF sul disco Windows — i 74 file sono tutti CRLF).
  Cambiarne uno senza l'altro farebbe risultare modificato mezzo progetto al primo salvataggio.
  *Nota di metodo: questa voce è rientrata tre volte perché ogni volta si correggeva il file
  invece della causa. Un difetto che ricompare dopo essere stato "chiuso" non è stato chiuso:
  è stato spostato più avanti nel tempo.*
- **Campi dati dichiarati e mai letti** (censiti 06/08/26). Due asset ne hanno:
  - `MovementSettingsSO`: `ChargeBumpDistance/Duration`, `SkirmishBumpDistance/Duration`.
    ⚠ In più, l'asset **non contiene `_hitReactionDistance`**: il campo esiste nel codice
    ma non è mai stato serializzato. Riaprire l'asset in Inspector e ridargli un valore.
  - `HexTypeSO`: **`IsRedZone`, `ModifierA`, `ModifierB`** — dichiarati, esposti da
    proprietà pubbliche, zero lettori in tutto `Assets/Script`. ⚠ Attenzione: il
    documento diceva "Zona Rossa: non esiste". È vero come **regola**, ma il **campo
    dati c'è da mesi** — mi ci sono fidato e ho quasi accusato un revisore di essersela
    inventata. Quando si farà la Zona Rossa (priorità 2 del cap. 16), il campo c'è già.
    Non usarlo prima in un punto isolato: implementeresti un decimo della meccanica in
    un posto dove nessuno andrà a cercarla.

  Testo originale sui campi Bump: definiti, esposti, mai letti da nessuno.
  NON è perché manchi l'animazione di ricezione colpo — quella ESISTE:
  `UnitMovement.PlayHitReaction`, chiamata da `ExecuteSkirmish` via il callback
  `onImpact`, e usa campi diversi (`HitReactionDistance`, `RecoilDuration`,
  `SkirmishAtkDuration`). I campi Bump sono residui di una nomenclatura
  precedente. Verificato 27/07/26.
- **TurnManager è un god script: 753 righe** (registrato 03/08/26, misurato 08/08/26).
  Contiene ciclo turni, carica, spinta, sfogo laterale, onda di panico, movimento,
  scontro, lancio, barricata, coro, sedersi, più nove canali evento serializzati.

  ⚠ **La direzione di refactor registrata a inizio agosto era sbagliata.** Diceva di
  estrarre gli esecutori per famiglia d'azione (`CombatExecutor`, `MovementExecutor`,
  `SpecialActionExecutor`). Quella divisione **sposta il codice senza separare niente**:
  l'architettura del progetto dice già che `TurnManager` **è** l'esecutore, quindi
  spezzarlo per famiglia dà tre file che fanno la stessa cosa più un livello di inoltro.

  **Il taglio giusto è uno solo, e sta nei numeri.** Delle 753 righe, la carica ne occupa
  323 (43%) — ma la carica vera sono 88 righe: le altre **235 sono la spinta**
  (`TryBuildPushChain`, `BuildMovesFromColumn`, `TryReleaseSideways`, `FindSideCell`,
  `CountAdjacentAllies`, `ApplyPushChain`, `ResolvePushOrRemove`, `PushResolution`,
  `ApplyPanicWave`). Quello non è un'azione, è un **sottosistema**: vocabolario proprio
  (colonna, catena, sfogo, onda), un solo punto d'ingresso, un solo chiamante, nessuno
  da fuori che lo tocca.

  Proposta: `PushResolver`, **classe C# pura** — non un MonoBehaviour, non ha bisogno di
  stare in scena né di un ciclo di vita Unity, e così diventa collaudabile senza avviare
  il gioco. `TurnManager` lo costruisce in `Start` e scende a ~520 righe omogenee.

  **Non farlo mentre si aggiungono feature**: a bocce ferme, col gioco funzionante prima
  e dopo. ⚠ **Non sblocca niente**: nessun bug dipende da lui e la scena Assemblea non
  tocca `TurnManager`. Il guadagno è che la spinta è il sistema che ha prodotto più
  correzioni in tre giorni (l'ordine di `moves`, la cattura della cella, l'invariante di
  `CountAdjacentAllies`) e si ragiona meglio in 235 righe con un nome che dice cosa fa.
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
numeri sono già fuori scala. Proposta del cap. 17.8: Operai 6, Anarchici 9,
Black Bloc 9, Studenti 12, Pacifisti 18–24 — stesse proporzioni, ma 3
punti diventano una ferita e non una condanna.
⚠ **Non è più un prerequisito del panico** (dal 06/08/26): il danno è uscito dalla
propagazione, quindi il panico si può implementare e provare su questa scala. Va alzata
per lo **scontro**, non per il panico, e conviene farlo **dopo** avendo il panico in mano.

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

## ORDINE DI LAVORO CONCORDATO (04/08/26, aggiornato 16/08/26)
**spinta a domino (FATTA 05/08) → passata di fix (FATTA 06/08) → B3+B7, query condivise
(FATTE 08/08) → panico (FATTO 08/08) → leggibilità (FATTA 10/08) → obiettivi (FATTI
14/08) → punti di ritrovo e spawn a runtime (FATTI 16/08) → presidio polizia (FATTO 18/08) →
scena Assemblea → Zona Rossa.**

⚠ **Il presidio è passato DAVANTI all'Assemblea** (18/08/26, su richiesta di Edoardo).
Motivo: dopo il playtest del 16/08 ("è molto difficile perdere", "la polizia è ancora
scema") non aveva senso costruire la fase di preparazione a una partita che non oppone
resistenza. Il rework pesante della polizia — inventario, tipi di unità (cellulari,
piantoni) — resta invece **dopo** l'Assemblea, come concordato l'08/08.

⚠ **La catena di dipendenze è cambiata il 13/08 e va letta in quest'ordine**:
`19 obiettivi → 20 Assemblea → 8 presidio → 5.6 Zona Rossa`. Gli obiettivi sono passati
davanti a tutto perché "dichiarare un obiettivo" ha bisogno di qualcosa a cui puntare e il
guinzaglio della polizia ha bisogno di un'ancora. La Zona Rossa, che il cap. 16 dava come
"priorità 2, la più economica", è finita in fondo: non funziona senza il presidio.

**Prerequisiti dell'Assemblea: erano quattro, ne restano due.**
✅ `ObjectiveSO` (14/08) · ✅ coordinata di partenza + istanziazione a runtime (16/08) ·
❌ campi costo sugli SO · ❌ passaggio di stato fra scene.

⚠ **Il refactor di `PoliceAI` si è spostato DOPO la scena Assemblea** (concordato
08/08/26): l'Assemblea cambia cosa l'IA si trova davanti, quindi rifarla prima
significherebbe rifarla due volte. Vedi la sezione 1-bis.

⚠ **La passata di fix si è infilata davanti al panico ed è concordata.** Il triple
check del 06/08/26 ha trovato quattro bug attivi, uno dei quali (input del giocatore
durante il turno polizia) corrompe lo stato della griglia e ne rende raggiungibile un
altro. Scrivere il panico sopra una griglia che può essere mutata da due parti
contemporaneamente significa non poter distinguere un bug del panico da un bug
preesistente. Lista e ordine in `D:\GDDRIOT\FIXLIST_2026-08-06.md`;
le prime otto voci sono mezza giornata e chiudono tutto ciò che è attivo.

Gli SFX più sotto restano validi ma NON sono il prossimo passo: si fanno quando
esisteranno gli eventi di gameplay da agganciare.

### 1. ~~Panico~~ — ✅ **IMPLEMENTATO l'08/08/26**
Il comportamento reale è documentato in **PARTE 1, sezione "Panico"**. Quello che segue
è il piano di design con cui è stato scritto: si tiene perché contiene i *perché*, ma
per sapere cosa fa il codice si guarda la PARTE 1.

Differenze fra il piano e ciò che è stato fatto:
- **Chi va in panico è chi SUBISCE la carica, non chi la perde.** L'08/08 la carica ha
  smesso di confrontare Atk e Def, quindi un perdente non esiste più. È la prima stesura
  del GDD 17.4, che era stata scartata perché una carica fallita sarebbe stata gratis:
  senza carica fallita, il buco non c'è.
- **La riga nel pannello e la tinta sono arrivate il 10/08/26** con `UnitStatusView`.
  Il panico è quindi **completo**: regola, propagazione, cura, e tre canali di lettura
  a schermo (tremore, tinta, testo).

Design originale in `D:\GDDRIOT\17-Coesione-Adiacenza-e-Panico.md` §17.4 e §17.6:
⚠ **Il design è stato rivisto il 06/08/26**: il danno è uscito dalla propagazione.

- Va in panico **chi PERDE** lo scontro di carica (superato l'08/08, vedi sopra).
  Si propaga **per contatto** lungo le adiacenze della stessa parte. Il
  decadimento si misura in **passi attraverso la folla**, NON in distanza esagonale —
  è quello che fa contare la forma del corteo.
- **Il Morale lo perde solo chi tocca la carica: −1**, la stessa cifra dello scontro.
  La propagazione porta **solo lo stato**. Il gradiente 3/2/1 è passato dal Morale alla
  **durata**: 3 turni a chi ha perso, 2 al passo 1, 1 al passo 2, poi l'onda si spegne.
  *(Prima erano −3/−2/−1 di Morale: con la scala attuale uccidevano tre gruppi su cinque
  senza che avessero combattuto, e legavano il panico al rifacimento della scala.)*
- **La causa del −1 dev'essere quella del combattimento** (`CauseFrom(atk)`), non una
  causa "panico": se quel punto porta a zero uno spezzone caricato dalla polizia deve
  essere un **arresto**. Conseguenza: `MoraleLossCause.Panic` resta senza utilizzatori.
- **Il panico uccide lo stesso, ma indirettamente**: chi è in panico non riceve aure,
  quindi il prestito di Morale rientra e chi reggeva solo grazie ai vicini cade. È
  `ApplyAuraMorale` + il `do...while` di `ApplyAuras` — già scritti, zero codice nuovo.
- Ordine obbligato: **−1 con le aure ancora attive → flag di panico → `RefreshBoardState`**.
  Il ricalcolo fa il resto da solo, crollo a catena compreso.
- **Chi è già in panico**: non paga di nuovo, l'onda **lo attraversa** (occupa il suo
  passo nella scala), e la durata si aggiorna con `Mathf.Max(attuale, nuovo)` — mai
  con l'ultimo valore, altrimenti un'ondata debole *cura* chi stava peggio.
- Durante il panico l'unità **non dà e non riceve aure**. Si muove e agisce
  normalmente (versione permissiva, si stringe solo se serve).
- **Durata: 3 turni il corteo, 1 turno la polizia** (loro sono organizzati, si
  riformano). ⚠ **Il "punto unico" del 04/08 era sbagliato**: decrementando tutto in
  `ExecutePoliceTurn`, uno spezzone che va in panico durante il turno polizia perde
  subito un turno di panico senza averlo giocato. Il decremento va **a fine turno della
  propria parte** — spezzoni in `EndTurn`, polizia in `ExecutePoliceTurn` — cioè
  **dove si ricaricano i PA di quella parte**.
- **Seduto = frangifuoco**: non entra in panico e **interrompe la catena**.
- **Chi è in panico NON può sedersi.** Senza questa regola siediti+rialzati (3 PA)
  azzera tre turni di panico. Il Coro resta l'unica cura anticipata.
- Serve la **visualizzazione**. ⚠ **Al 06/08 `UnitStatusView` NON esisteva** (il documento
  lo dava per esistente: era falso). **Esiste dal 10/08/26** — il comportamento reale sta
  in PARTE 1, "Animazione e feedback visivo". Piano concordato: tremore laterale in
  loop via DOTween su `_graphicsTransform` in **X** (`DOLocalMoveX`, yoyo, `Ease.Linear`,
  ~0,04 unità e ~0,05 s), campo `_panicTween` **separato** da `_movementLoopTween` —
  altrimenti `StartBobLoop` lo uccide al primo movimento. L'asse X è libero: i movimenti
  scrivono su `_rootTransform`, il bob su `_graphicsTransform` in Y, il flip sulla scala.
  Sincronizzazione da `UnitsRenderer.UpdateView`, che è già la funzione "allinea la vista
  allo stato" — e va spento anche nel ramo `!IsAlive`, prima di `SetActive(false)`.
- Ordine di lavoro concordato: **stato + tremore → soppressione aure → propagazione →
  aggancio in `PushResolution` → decremento e pannello.** Ogni pezzo è provabile da solo.
- ⚠ **Nell'aggancio, l'ordine è insidioso**: chi perde la carica può essere **rimosso**
  dalla spinta. Quindi prima si risolve la spinta, poi — se è ancora vivo — il −1, poi
  l'onda. E l'onda va propagata da una **cella catturata prima**, non da
  `perdente.PositionCell`: se il −1 lo uccide, quel campo punta a una cella già liberata
  e ci si appoggerebbe a un bug noto invece che a una garanzia.

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

~~⚠ **La scala del Morale va alzata insieme al panico**~~ — **NON PIÙ UN PREREQUISITO
dal 06/08/26.** Era vero con lo shock a 3/2/1 sulla propagazione; adesso l'unico danno è
un −1 a chi tocca la carica, che nessuno dei cinque gruppi rischia di non sopravvivere
(Operai 2→1 è il caso peggiore). La scala va comunque alzata (proposta 17.8:
6/9/9/12/18-24) ma **per il bilanciamento dello scontro**, e si può fare dopo, avendo il
panico in mano per misurarlo. Erano due cambiamenti rischiosi legati insieme e nessuno
dei due provabile da solo: adesso sono separati.

### 1-bis. Refactor di PoliceAI — DOPO la scena Assemblea (concordato 08/08/26)
L'IA della polizia va rifatta **con azioni e tipi di unità diversi**, non ritoccata.
Motivo del rinvio: la scena Assemblea introduce la composizione del corteo e
l'istanziazione a runtime, quindi cambia cosa l'IA si trova davanti. Rifarla prima
significherebbe rifarla due volte.

**LA PREMESSA DI DESIGN DEL REFACTOR (formulata 13/08/26).** Prima di riscrivere una
riga, questo è il punto da cui partire:

> **L'albero decisionale di `PoliceAI` codifica ancora il modello di combattimento
> pre-08/08.** È fatto così: `distanza 1 → scontro`, `distanza 3 → carica`,
> `altro → avvicinati`. È uno smistamento **per distanza**, e presuppone che a ogni
> distanza esista una sola cosa sensata da fare — vero quando la carica era "un attacco
> forte", falso da quando spinge e basta.
>
> Le due azioni adesso hanno **scopi diversi, non gradi diversi**: lo **scontro logora**
> e vuole statistiche favorevoli; la **carica sposta** e funziona sempre. Quindi la
> domanda giusta non è *"quanto è lontano il bersaglio"* ma **"voglio fargli male o
> voglio spostarlo?"**. Contro un muro di Operai la risposta è la seconda, sempre,
> indipendentemente dai numeri.
>
> **Conseguenza che nessuno aveva scritto** (Edoardo, 13/08/26): la polizia può caricare
> gli Operai **non per ucciderli ma per spostarli**, e aprire un varco verso un'unità
> che invece può battere in mischia. Il muro non è più una barriera assoluta: è un
> problema di posizionamento.
>
> ⚠ **Questo comportamento è per forza un piano su DUE turni, e per questo obbliga al
> refactor invece che a una toppa.** La polizia ha 5 PA, la carica ne costa 4, arretrare
> in corsia di rincorsa ne costa almeno 2: 6 > 5, non entra in un turno. Aprire un varco
> richiede quindi che l'IA **ricordi un'intenzione fra un turno e l'altro** — cioè abbia
> uno stato. Oggi è una funzione pura che riparte da zero a ogni chiamata.
>
> E richiede di valutare una plancia **ipotetica** ("se spingo X di una cella, Y diventa
> raggiungibile?"), cioè una query di pathfinding su uno stato che non esiste ancora.

Quello che si accumula fino ad allora, da affrontare in blocco:
- ✅ ~~**Non cerca bersagli alternativi**~~ e ~~**a distanza 1 rinuncia al turno**~~ —
  **chiuso il 13/08/26** con un ripiego a **due passate**: prima si cerca su *tutti* i
  bersagli qualcosa che faccia male o che sposti (scontro favorevole, oppure carica
  legale), e solo se non c'è niente si ripiega sull'avvicinamento.
  ⚠ **Le due passate non sono cosmesi.** Con una passata sola, valutando i bersagli in
  ordine di distanza, l'avvicinamento — l'azione più debole — vinceva solo perché il suo
  bersaglio veniva prima in lista: Operaio a distanza 1 saltato, Studente a distanza 2
  raggiunto, e un Operaio a distanza 3 **caricabile** non veniva mai nemmeno guardato.
  ⚠ **Effetto collaterale voluto**: adesso la polizia davanti al muro di Operai se ne va
  a cercare un bersaglio più morbido. È corretto (la repressione va sui bersagli
  morbidi) e rende il muro uno scudo che funziona, ma significa che il giocatore può
  **escare** la polizia scoprendo un Pacifista. Diventa una leva vera col coro
  provocatorio: è la stessa idea vista dall'altro lato.
- **Non guarda `_allowedActions`**: la maschera della polizia (oggi 1 = Charge) non ha
  nessun effetto, perché `CanPerformAction` è controllato solo in `InputHandler`.
  ⚠ Prerequisito di qualunque azione nuova della polizia: finché la maschera è inerte,
  aggiungere lacrimogeni o scudi significa darli a tutti e sempre.
  🔴 **Confermato dalla revisione esterna del 20/08/26, e il problema è più grosso di così**:
  ✅ **CHIUSO il 20/08/26**: `CanPerformAction` è ora dentro `TacticalQuery.GetValidTargets`,
  `CanThrow`, `CanPlaceBarricade` e `TurnManager.CanCharge`. In `InputHandler` il controllo
  resta ma **spiega** invece di decidere. Diagnosi originale, conservata perché la forma
  del difetto torna:
  `_allowedActions` **non è una regola, è un filtro dell'input**. `CanPerformAction` vive
  solo in `InputHandler`; `TurnManager.CanCharge` non lo consulta e `PoliceAI` chiama
  `CanCharge` direttamente. Quindi *qualunque* azione invocata da codice invece che da un
  bottone scavalca la maschera. Viola "una decisione, un posto solo": è una domanda di
  legalità che vive nello strato UI invece che in `TacticalQuery`.
- 🔴 **Oscilla fra due celle** quando non può vincere nessuno scontro (visto in playtest il
  20/08/26 contro un muro di Pacifisti seduti, Def 1+5+2 = 8 contro Atk 8). La passata 1 non
  trova niente, la passata 2 si sposta di una cella, e da lì la "cella migliore" torna a
  essere quella di prima: avanti-indietro finché finiscono i PA. Tamponato con un
  `visitedThisTurn` che rifiuta una destinazione già visitata nel turno.
  ⚠ **È un tampone, non la soluzione**: la risposta di design è che contro un muro
  invalicabile in mischia la polizia **arretri e carichi** (la carica spinge e basta), ma
  costa più di 5 PA ed è quindi un piano su due turni — serve memoria fra i turni.
- **Nessuna nozione di formazione**: cordone, blocco, escalation sono la priorità 4 del
  cap. 16 e non esistono.
- **Nessuna memoria fra turni**: vedi la premessa qui sopra. È il pezzo che trasforma il
  refactor da riordino a lavoro vero.

### 1-ter. Le unità sono posizionate nel MONDO, non in coordinate (scoperto 08/08/26)
`UnitsSetup.Initialize` fa `_grid.WorldToGrid(transform.position)`: la cella su cui nasce
un'unità è **dedotta da dove l'hai trascinata nell'editor**, guardando i gizmo degli
esagoni. Non esiste da nessuna parte un campo "coordinata di partenza".

Scoperto per caso spostando `MapManager` a (5,3,0) per provare il fix di `GridToWorld`:
gli esagoni si sono spostati, le unità no — perché **non sono figlie di `MapManager`**,
sono GameObject indipendenti in scena. Il risultato è coerente (la griglia si è mossa
sotto di loro, quindi ora stanno su celle diverse) ma rende evidente l'accoppiamento.

⚠ **È un prerequisito nascosto della scena Assemblea.** Quella istanzia le unità a
runtime, e a quel punto "su quale cella nasce" non può più essere "dove l'ho messa
nell'editor". Serve un `UnitsSetup` che riceva una coordinata, o qualcosa che lo affianchi.
Va aggiunto ai quattro prerequisiti della sezione 2.

*(Nota: la prova "sposta la griglia e guarda se si rompe" non è eseguibile su questa
scena finché le unità non sono figlie di `MapManager`. Il fix di `GridToWorld` è
verificato in altro modo: le diciassette conversioni passano tutte per due metodi,
quindi non possono più essere in disaccordo fra loro.)*

### 2. Scena Assemblea — design ampliato il 13/08/26, vedi GDD cap. 20
`D:\GDDRIOT\20-Assemblea-e-Volantino.md` (nuovo). L'Assemblea **non è solo comporre il
corteo**: nella stessa fase si scrive il **volantino**, che decide l'**appuntamento**
(quindi le celle di partenza — chiude la domanda mai risolta del GDD cap. 2) e gli
**obiettivi dichiarati** (quindi la condizione di vittoria, che sostituisce "accumula 30
punti"). Più le azioni assembleari: comunicato stampa (più giornalisti in campo), azione
legale (libera un arrestato).

Prerequisiti tecnici — **sono quattro, non cinque**: coordinata di partenza e
istanziazione a runtime **sono lo stesso lavoro** (con lo spawn a runtime la posizione nel
mondo non esiste prima, quindi la coordinata deve arrivare da fuori per forza).
1. coordinata di partenza su `UnitsSetup` + istanziazione a runtime;
2. `ObjectiveSO` — senza obiettivi come entità, "dichiarare un obiettivo" non ha oggetto
   (GDD cap. 19);
3. campi costo sugli SO;
4. passaggio di stato fra scene (solo andata per la v1).

⚠ **Il taglio v1/v2 è quello che tiene fuori lo strato run.** v1 = componi, equipaggia,
volantino, comunicato stampa, esito graduato. v2 = punti che si accumulano fra livelli,
azione legale, firma del manifesto, dissenso interno — tutta roba che richiede il
`RunManager`, che non esiste ed è registrato come trappola.

⚠ **Incoerenza numerica aperta**: questo file diceva "1000 punti fissi" per la
composizione, ma il GDD 10.5 parla di Punti Reclutamento con budget iniziale **15** e costi
unità fra 3 e 5. Il 1000 non ha fonte nel GDD. Da unificare prima di implementare.

⚠ **Il bug dei costruttori Runtime diventa raggiungibile** con la lista di celle di
partenza: oggi serve un errore di trascinamento nell'editor, domani basta una coordinata
ripetuta in un elenco. Va chiuso insieme al prerequisito 1 (vedi bug noti).

### 3. Obiettivi — design chiuso, e non è più l'ultimo della fila
`D:\GDDRIOT\19-Obiettivi-e-Occupazione.md`. Occupazione per turni consecutivi, obiettivo
rivendicato che non paga più, obiettivi configurabili via `ObjectiveSO`.

⚠ **Promosso il 13/08/26: è diventato prerequisito di due capitoli.**
- La **scena Assemblea** deve poter *dichiarare* un obiettivo, e senza `ObjectiveSO` non
  c'è niente a cui puntare.
- Il **presidio della polizia** (GDD cap. 8, riscritto) ancora il guinzaglio "all'obiettivo
  che difende": con 35 celle obiettivo indistinte quell'ancoraggio non esiste.

La catena di dipendenze del lavoro di design del 13/08 è quindi: **19 → 20 (Assemblea) →
8 (presidio) → 5.6 (Zona Rossa)**. La Zona Rossa era marcata "priorità 2, la più
economica": non lo è più, perché non funziona senza il presidio.

Aggiunte del 13/08 al cap. 19: entrare in un obiettivo alza la Repressione e fa scattare
l'Allarme **all'ingresso, non a occupazione completata** (altrimenti l'ultimo obiettivo del
livello è gratis); esito graduato **rivendicato / raggiunto / mancato**; e la domanda
aperta "la polizia può riprendersi un obiettivo?" è chiusa con un no.

## Poi: SFX — il blocco è sciolto (aggiornato 08/08/26)
Il sistema audio è pronto e testato, e i canali di combattimento adesso sono **collegati
in scena**. `TurnManager` alza `_skirmishWin/Lose/Par` (in `RaiseCombactResult`, dentro
`onImpact`) e **un solo `_chargeEvent`** in `PushResolution` — dall'08/08 la carica non
ha più esiti, quindi i tre canali Win/Lose/Par della carica sono spariti insieme a
`RaiseChargeResult`.
~~⚠ **Due asset restano orfani a disco**: `LoseChargeEvent` e `ParChargeEvent`. Da
cancellare, e `WinChargeEvent` da rinominare in `ChargeEvent`.~~ — **FATTO, verificato
13/08/26.** A disco esiste solo
`Assets/ScriptableObjects/Events/Turn/CombactEvents/ChargeEvent.asset`, e in
`LVLTest.unity` il campo `_chargeEvent` punta al suo guid (`cae3f1f7…`).
Restano senza evento: movimento, coro, sedersi, lancio, barricata, dispersione, **panico**.
Ordine di lavoro:
1. ~~Aggiungere i campi `[SerializeField] GameEventSO` in `TurnManager`, alzarli, creare
   gli asset e collegarli~~ — **FATTO** per scontro e carica il 06-08/08/26.
2. Creare i canali mancanti (movimento, coro, sedersi, lancio, barricata,
   dispersione) come asset in `ScriptableObjects/Events/`, e i campi corrispondenti.
3. Solo allora creare gli `SFXSO` e infilarli nell'array `_sfxevents`. Il
   collegamento è tutto da Inspector, zero codice nuovo.
Nota di design: conviene un evento per *esito* (Win/Lose/Par) più che per *azione*,
così lo stesso scontro può suonare diverso a seconda di come va.
⚠ Licenze: freesound NON è tutto CC0, è un misto CC0/CC-BY/CC-BY-NC. "Placeholder"
non è una categoria che esiste nel diritto d'autore: se la build è pubblica,
l'attribuzione scatta. Filtrare solo CC0 e tenere la lista fonti da subito.
✅ **Verificato 13/08/26**: nella build pubblica **v0.19.2 non è stato usato audio non
royalty-free** (confermato da Edoardo). Il rischio non si è materializzato. Ma la
pubblicazione è **già avvenuta**, quindi da qui in avanti ogni clip aggiunta è
pubblicata il giorno stesso in cui si carica una build: la lista fonti va tenuta da
adesso, non da quando sarà lunga.
*(`D:\GDDRIOT\_RawAudio\Crowd` esiste ma il lavoro audio NON è iniziato — verificato
13/08/26. Non trattarla come materiale in lavorazione.)*

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
- ✅ **RISOLTO 10/08/26 — l'unità seduta adesso si vede.** Tinta blu sulla griglia
  (`UnitStatusView`) più la riga `SEATED` nel pannello. Restava aperta da una settimana
  ed era diventata pericolosa: un seduto interrompe la catena del domino, quindi può far
  arrestare chi gli sta davanti — l'unica regola del gioco in cui la scelta di un'unità
  uccide un'altra unità, e fino a ieri non era visibile.
  ⚠ **La tinta è un ripiego consapevole**: un colore dice "questo è diverso", non "questo
  è seduto". La soluzione vera è uno **sprite dedicato**, e dipende dalla direzione
  artistica. Quando arriverà, la tinta va tolta — non sommata.
- **Il pannello statistiche non si aggiorna quando cambia il vicinato.** `Refresh`
  scatta alla selezione e ai cambi turno, non quando un'altra unità si sposta: se
  muovi un Operaio accanto allo spezzone selezionato, il bonus d'aura mostrato resta
  vecchio finché non deselezioni. Si risolve chiamando `Refresh` dallo stesso punto in
  cui si ricalcolerà la Coesione (dopo movimento, spinta, dispersione).
- **Il bonus da seduto non è ancora scomposto nel pannello**: `SpezzoneRuntime.Def`
  restituisce `Def + 5` da seduto, quindi il +5 è dentro il numero base e non è
  distinguibile dall'aura. Dal 10/08 la riga `SEATED` almeno **dice perché** quel numero
  è più alto, che era metà del problema. Per separarlo davvero servirebbe scomporlo alla
  fonte — cioè togliere il `+5` dall'override di `Def` e farne un modificatore dichiarato,
  sulla stessa strada delle aure.

## Arretrato precedente
9. Animazione ricezione colpo del difensore: FATTA per lo scontro
   (`PlayHitReaction` via `onImpact` in ExecuteSkirmish). ⚠ **La "distinzione win/lose"
   non ha più oggetto per la carica** (dall'08/08 non ha esiti) e per lo scontro è già
   implicita: `onImpact` fa lampeggiare **chi ha effettivamente perso Morale**, letto
   dalla lista `hit`. Resta da fare la **reazione al colpo per la carica**, che va agganciata
   dentro `PushResolution`/`ApplyPushChain` — non in `PlayCharge`, che parte quando la
   destinazione del difensore non è ancora decisa. Riverificato 10/08/26.
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

## Changelog sessione 39 (22/08/26) — resolver, servizi, e la prima rete di test
*Come sopra: ogni cosa sta nella sezione che la riguarda. Qui l'elenco e dove guardare.*

- 🟢 **Estrazione delle regole dagli esecutori** → sezione "Resolver, servizi e test".
  Sei resolver e due servizi, tutti statici puri e senza `Debug.Log`. Da 74 file a 98.
- 🟢 **82 test in EditMode** che asseriscono le regole documentate, non l'implementazione.
- 🟢 **`UnitActionPresenter`**: le animazioni escono da `TurnManager` e il buco del timeout
  sulla carica si chiude **per costruzione** — `PushResolution` è dopo lo `yield`, non più in
  una callback. `ResolveOnce`/`FinalizeOnce` sono spariti perché non servono più.
- 🟢 **Validazione della configurazione all'avvio**: un solo `LogError` con tutti gli errori,
  poi il livello non parte. Sostituisce i `LogWarning` sparsi che lasciavano proseguire.
- 🟢 **`CanPerformAction` chiuso anche su Chant e SitStand** (`UnitActionResolver`): non più
  "sicuro per call graph" ma per contratto. Era la coda segnalata da tutte e tre le revisioni.
- 🟢 **Tutti e sette i `TrySpendActionPoint` controllano l'esito**, carica compresa.
- 🔴 **Regressione mia, trovata dalle revisioni**: passando `CanPerformAction` dentro
  `CanThrow`/`CanPlaceBarricade` avevo **cancellato il controllo dei PA**. Lancio e barricata
  erano gratis a 0 PA. ⚠ **Quarta volta** che una sostituzione di blocco si porta via una
  guardia — la regola era già scritta e l'ho violata dando un frammento che sembrava completo.
- 🔴 **Doppia sottoscrizione di `OnLeftClick`**, stessa causa: una riga di contesto in un
  frammento, aggiunta invece che sostituita. Il protocollo a due clic diventava a un clic.
- 🔴 **`FindReachablePostCell` cercava le celle dell'obiettivo invece dell'area del guinzaglio**
  → sezione "Presidio". Due definizioni diverse di "sono al mio posto".
- 🔴 **`_requiresSimultaneous` era un'eccezione alla finestra di due turni** → sezione
  "Obiettivi". Adesso è solo un cancello.

**Le due lezioni della tornata:**

1. **Un frammento di patch con righe di contesto è un invito a duplicare.** Due dei tre bug
   attivi di oggi nascono da lì, non dal ragionamento. Chi passa codice da incollare deve dare
   **il metodo intero**, oppure dire esplicitamente cosa va sostituito e cosa no.
2. **La revisione che non trova niente è la meno affidabile.** Delle tre, quella che ha
   concluso *"il codice è robusto, le falle effettive sono chiuse"* girava su un albero che
   conteneva due bug attivi. Le altre due li hanno trovati entrambe, indipendentemente.

## Changelog sessione 38 (20/08/26) — le revisioni esterne, e il presidio che funziona davvero
*Voce corta di proposito: ogni cosa è documentata **nella sezione che la riguarda**, e
duplicarla qui è il difetto già segnalato in cima al file. Qui c'è solo l'elenco e dove
guardare.*

- 🟢 **Volantino pubblico** → sezione "Presidio, guinzaglio e allarme".
  `ReinforceDeclaredObjective` sposta una quota del presidio libero sull'obiettivo dichiarato.
- 🟢 **Report di copertura** → `LVLManager.LogCoverageReport`. Ha già prodotto due risultati:
  ha **falsificato** l'ipotesi degli obiettivi secondari come diversivo (presidio 0 e 9-17
  turni di cammino: nessuno da svegliare e nessuno ci va), e ha smontato una mia conclusione
  troppo pesante sulla mappa, che avevo tratto usando solo il passo del più lento.
- 🟢 **`Required = Cells + 1`** → sezione "Obiettivi".
- 🔴 **Tre bug che paralizzavano il presidio** → sezione "Presidio": rientro impedito dal
  guinzaglio, `NearestPostCell` che sceglieva una cella occupata, hard lock su `_policeAI`.
- 🔴 **Allarme muto su due aggressioni su tre** → sezione "Presidio", `ReportAggression`.
- 🔴 **Oscillazione di `PoliceAI`** → sezione 1-bis.
- 🟢 **Codifica chiusa alla radice** (BOM + `.editorconfig`) → bug noti.
- 🔴 **"Logica prima, animazione dopo" non è un vincolo rigido** → Architettura.

**La lezione della giornata, e vale oltre il codice:** cinque dei sei difetti erano
**conseguenze agganciate agli esecutori invece che ai fatti** — l'allarme scritto dentro un
solo esecutore, l'intrusione controllata in un solo punto d'ingresso, `CanPerformAction` che
vive nell'input. Quando una regola vale "ogni volta che succede X", il posto giusto è dove X
è nominato, non nei posti da cui X capita di passare oggi.

## Changelog sessione 36 (16/08/26) — doppia revisione esterna e passata sui failure path
Nessuna regola nuova. Tutti i difetti trovati stavano **nei bordi**: percorsi di
fallimento, listener sincroni, componenti validati a metà.

- 🔴 **Il fix del 10/08 su `_statusText` era a metà.** Guardia aggiunta sopra,
  assegnazione non protetta lasciata sotto. Vedi la sezione "Il pannello unità esiste in
  DUE copie". È il difetto più grave della tornata perché riproduce l'hardlock del turno
  polizia, e perché il codice **sembrava** corretto.
- 🟢 **Il timeout dello scontro adesso finalizza.** `UpdateView` e `RefreshBoardState`
  vivevano solo in `onComplete`: allo scadere dei 5 secondi il Morale era già stato
  applicato ma la plancia restava non aggiornata — morti non nascosti, aure non
  ricalcolate, Coesione vecchia. Introdotto `FinalizeOnce()`, **stesso pattern di
  `ResolveOnce()` nella carica**, chiamato sia dalla callback sia dal timeout.
- 🟢 **Chiuso l'ultimo `WaitUntil` senza via d'uscita** (`BootManager`, attesa del primo
  frame video). Adesso tutti e cinque ne hanno una.
- 🟢 **Due validazioni incomplete chiuse**: `ThrowObjectVFX` non validava
  `_itemSelectedEvent` e `InventoryView` non validava `_actionSelectedEvent`, entrambi poi
  dereferenziati in `OnEnable`. Un campo mancante nell'Inspector produceva una
  `NullReferenceException` **dopo** che altre due sottoscrizioni erano già andate a buon fine.
- 🟢 **`SpawnRoster` indurito**: istanza distrutta se `Initialize` fallisce, `spawned`
  incrementato solo a registrazione riuscita, log duplicato rimosso.
- 🟢 **La regola `IsAlive` è ora rispettata ovunque**: zero confronti diretti con
  `UnitsStatus.Alive` in tutto `Assets/Script`. L'ultimo era in `TacticalQuery.GetAuraBonus`,
  ed era il più pericoloso: con uno stato vivo nuovo (ferito, immobilizzato) un'unità viva
  avrebbe smesso di **dare aura**, alterando Atk, Def e il prestito di Morale di tutti i
  vicini. L'altro, nell'highlight del Coro, avrebbe prodotto una divergenza
  highlight/esecuzione, perché `ExecuteChant` usava già `IsAlive`.
- 📖 **Registrati come rischi noti** (non corretti, di proposito): `ApplyPushChain` non
  transazionale, rientranza di `WinLevel` nel ciclo degli obiettivi, timeout del movimento
  IA che non annulla il movimento, `CanCharge` fuori da `TacticalQuery`.

**Sul processo di revisione, tre cose che valgono più dei bug:**

1. 🔴 **Una revisione è stata fatta sul dump sbagliato.** Il primo giro di ChatGPT è
   arrivato con sei "bug attivi" — **tutti e sei già chiusi fra il 6 e il 16 agosto**,
   perché aveva letto lo snapshot del 6. Il revisore ha però **dichiarato la fonte in
   apertura** e ha rifiutato di rispondere alle domande sul codice che non aveva: la regola
   *"cita la fonte e separa quello che sai da quello che deduci"* ha funzionato. Il difetto
   era nell'input, non nel revisore.
   ⚠ Contromisura aggiunta al `CODECHECK`: una riga che dichiara **quanti file e quante
   righe** contiene il dump giusto, così il revisore può accorgersi da solo di averne uno
   vecchio *prima* di scrivere una revisione intera.
2. 🔴 **Il mio documento di revisione conteneva una premessa falsa.** In D5 avevo scritto
   "tutte le attese hanno un timeout": non era vero (`BootManager`), ed è pure una cosa che
   era **già registrata** fra i bug noti. Seconda volta in due giorni dopo le tre premesse
   sbagliate nel `DESIGNCHECK`. Entrambi i revisori l'hanno trovata, il che conferma che
   chiedere di controllare le premesse serve.
3. 🔴 **Due sostituzioni di blocco si sono portate via una guardia.** Prima
   `if (setup == null)` in `SpawnRoster`, poi il rinomina che aveva lasciato
   `Type.IsObjectiveGround` su due righe di `TurnManager`. In entrambi i casi il codice
   sostitutivo veniva da qui e ometteva un pezzo che c'era.
   ⚠ **Regola: quando si sostituisce un blocco intero, si rilegge cosa conteneva prima.**
   Una guardia che sparisce non fa rumore.

## Changelog sessione 35 (14-16/08/26) — obiettivi, ritrovi, spawn del corteo
Due giornate di codice. Chiusi due dei quattro prerequisiti della scena Assemblea.

- 🟢 **Sistema obiettivi** (cap. 19): `ObjectiveSO` + `ObjectiveRuntime` + flood fill in
  `HexGrid` + `LVLManager` riscritto. Il punteggio non esiste più: **si vince rivendicando
  l'obiettivo dichiarato**. Occupazione a **celle-turno**, accumulo che si azzera se lasci
  la presa. Vedi la sezione dedicata in PARTE 1.
- 🟢 **Punti di ritrovo e spawn a runtime** (cap. 20): `MeetingPointSO` +
  `MeetingPointRuntime`, `FloodGroup` generalizzato, `UnitsSetup` che riceve griglia e cella.
  **La capienza della piazza è il limite del corteo.**
- 🟢 **Chiuso il bug dei costruttori Runtime**: `Vacate(unit)` condizionale e verifica
  prima di costruire. Era latente da settimane; la lista di celle di partenza lo rendeva
  raggiungibile per refuso invece che per errore di trascinamento.
- 🟢 **Strumenti di editor**: etichette delle coordinate sui gizmo con filtro per zoom e
  inquadratura (su una mappa grande `Handles.Label` per ogni cella impianta l'editor),
  colore per obiettivo, `ObjectiveLabelView` in world space con il progresso di occupazione.
- 🔴 **Il rinomina `_isObjective` → `_isObjectiveGround` avrebbe cancellato la mappa.**
  Unity serializza per nome: senza `[FormerlySerializedAs]` i tre asset avrebbero perso il
  flag e gli obiettivi sarebbero **spariti in silenzio** su una mappa 51×35 dipinta a mano.
  Nessun errore, nessuna eccezione, solo un livello non più vincibile. Stesso stampo del
  cambio di numerazione dell'`ActionType` di agosto.
- 📖 **Il dato che aspettavamo**: `[OBJ] 24 cell(s) painted as objective belong to NO
  objective` alla prima esecuzione, poi zero dopo aver dichiarato tutti e dieci gli
  edifici. Le 35 celle non erano una colonna dipinta per sbaglio.
- 📖 **Design: la grafica non si appende alla logica** (deciso 15/08 su osservazione di
  Edoardo). L'aspetto di una cella dipende dal suo **intorno** — una strada è orizzontale
  perché ha strade a est e a ovest — quindi è autotiling, e vive in uno strato separato che
  *legge* la logica. Conseguenza immediata: **il campo prefab sull'`ObjectiveSO` è stato
  rimosso**. L'esito della sessione è stato togliere codice, non aggiungerne.
  ⚠ Direzione: l'arte va **sotto** (sorting layer `BackGround`) con gli esagoni
  semitrasparenti sopra. Se stesse sopra, gli highlight sparirebbero e una deriva fra
  disegno e logica diventerebbe una **bugia** invece di un problema estetico.
- 📖 **Regola nuova nel cap. 19.7**: dentro un obiettivo non rivendicato non si può agire e
  si perde automaticamente ogni scontro; le difese sono **corpi che occupano i passaggi**,
  non un controllo di adiacenza. Dà alla Barricata un lavoro che non aveva e chiude la
  questione del sit-in senza una regola dedicata.
- 🔴 **Errore mio di metodo**: ho dichiarato che la scena non aveva più unità dopo aver
  cercato il **guid dello script `UnitsSetup`** dentro `LVLTest.unity`. Le istanze di
  prefab **non espandono i loro componenti** nello YAML della scena: registrano un
  riferimento al prefab più le modifiche. **Per sapere cosa c'è in una scena si cercano le
  istanze di prefab, non il guid di uno script.**
- 🔴 **La codifica è peggiorata**: da tre a **sette** file non-UTF8, tutti quelli in cui
  sono stati incollati commenti italiani con accenti. Visual Studio ricade sulla codepage
  di sistema sui file senza BOM.

**Due lezioni:**

1. **Rinominare un campo serializzato è un'operazione sui dati, non sul codice.** Il
   sintomo (non ci sono più obiettivi) è lontanissimo dalla causa (un nome cambiato), e
   non produce nessun errore. `[FormerlySerializedAs]` va messo *prima*, non dopo aver
   scoperto il danno.
2. **Un'astrazione va tolta quando si scopre che risolve il caso sbagliato.** Il prefab
   sull'obiettivo sembrava giusto finché non è emerso che il problema non erano gli
   obiettivi ma l'autotiling. La correzione buona è stata rimuovere, non generalizzare.

## Changelog sessione 34-bis (13/08/26) — sessione di design, zero codice
Solo GDD. Il codice non è stato toccato.

- 📖 **Cap. 8 (Polizia) riscritto.** La polizia passa da **minaccia a ostacolo**: presidia
  un obiettivo entro un raggio invece di inseguire. Presidio + Allarme locale (decade da
  solo, "il tenente" è il guinzaglio stesso) + Repressione con regole d'ingaggio per
  fascia + caserma e rientro. A repressione bassa **non attacca** se non viene attaccata o
  se qualcuno entra in Zona Rossa.
- 📖 **Cap. 20 (Assemblea e Volantino) nuovo.** Il volantino decide **appuntamento**
  (chiude la domanda del cap. 2 sullo schieramento, aperta e mai progettata) e **obiettivi
  dichiarati** (sostituisce "accumula 30 punti"). Firma e manifesto decidono la Repressione
  iniziale *e* cosa il corteo tollera: da lì il **dissenso interno**.
- 🟢 **Due meccaniche si sono chiuse a vicenda**: la Zona Rossa era ferma da mesi come
  "magnete tattico" senza un come. Il come è il presidio — **la Zona Rossa è l'eccezione
  che rompe il guinzaglio**. E il presidio senza la Zona Rossa sarebbe statico. Nessuna
  delle due funziona da sola.
- 🟢 **La diagnosi del cap. 16 ha una risposta.** "Il gioco non sa distinguere
  un'occupazione pacifica da una violenta": adesso sì, perché a repressione bassa
  l'avversario si comporta diversamente. Non è un punteggio appiccicato sopra.
- 🔴 **Metà del design proposto esisteva già nel GDD e stava per essere riscritto.**
  "Azione legale" e "assaltiamo le prigioni" sono le due strade di 10.2 (Assolda un legale
  / occupa l'edificio prigione); "obiettivi bonus" è la voce *obiettivo secondario* di
  10.5; i pesi delle azioni sono la colonna Aggressività di 5.7. Trovati grepando **prima**
  di scrivere. È la stessa lezione del codice: cercare prima di aggiungere.
- 🔴 **Incoerenza numerica trovata**: "1000 punti fissi" per la composizione (qui) contro
  "budget iniziale 15" del GDD 10.5, con costi unità 3-5. Il 1000 non ha fonte nel GDD.
  Registrata in entrambi, non risolta.
- ⚠ **Confine dichiarato**: punti persistenti, azione legale, firma e dissenso richiedono
  il `RunManager`. Il cap. 20 è tagliato in **v1 (senza run)** e **v2 (con run)** apposta.
- 🔴 **CAMBIO STRUTTURALE DELLA VITTORIA.** Non si vince più accumulando punti entro N
  turni: **si vince completando l'obiettivo dichiarato nel volantino, e fallirlo fa perdere
  il livello.** Gli obiettivi secondari valgono solo Punti Reclutamento per l'Assemblea
  successiva. Le condizioni di sconfitta diventano due: obiettivo fallito e Coesione a zero.
- ⏸ **Il limite di turni è PARCHEGGIATO: per ora si sviluppa SENZA.** Il contatore in
  `LVLManager` resta ma non deve far perdere. ⚠ È un buco noto, non una svista: senza
  orologio niente obbliga ad avanzare, e la Repressione non copre il vuoto perché sale con
  le **azioni**, non col tempo — quindi il problema colpisce proprio la strada non violenta.
  Direzione annotata in GDD 20.4-bis (scadenza per obiettivo invece che per livello, e alla
  scadenza il corteo si assottiglia invece di perdere di colpo), **non decisa**: non si può
  tarare a tavolino senza sapere quanto ci mette un corteo ad attraversare una mappa.
- 📄 **Documento per la revisione esterna del design**:
  `D:\GDDRIOT\_ExternalReview\DESIGNCHECK_2026-08-13.md`. Otto domande numerate, la lista
  di cosa è deciso apposta e non va riscritto, i buchi già noti, e le catene di dipendenza.
  Da dare a revisori esterni al posto di far leggere tutto il GDD.

## Changelog sessione 34 (13/08/26) — allineamento dei documenti
Nessuna riga di codice toccata. Working tree pulito su `7d09d9edd`, solo `.claude/`
non tracciato.

- 📖 **Documento di Progetto portato al v28** (sess.33 mancava: `UnitStatusView`, riga
  di stato nel pannello, lampo al colpo, il campo non assegnato che bloccava la partita).
  Il v27 è stato archiviato correttamente. Registrata in entrambi i documenti la storia
  del **v26 mancante**, che è un errore di archiviazione e non un file perso.
- 🔴 **Cinque percorsi sbagliati in questo file**: puntavano a `D:\UnityProject\GDDRIOT\`,
  che non esiste. Corretti. Nota di metodo: erano incoerenti *dentro lo stesso file* —
  alcune voci dicevano già `D:\GDDRIOT\` — e nessuno se n'era accorto perché un percorso
  in un documento non fallisce mai rumorosamente, a differenza di un percorso nel codice.
- 🟢 **Verifica di compilazione su tutto `Assets/Script`**: graffe, parentesi e
  `#region` bilanciati in ogni file.
- 🟢 **Chiusa la voce sugli asset evento della carica**: a disco resta solo
  `ChargeEvent.asset`, cablato in `LVLTest.unity`. Era data ancora per aperta.
- 🔴 **La codifica non-UTF8 è rientrata da una terza porta**: adesso è `UnitsRenderer.cs`.
  La voce è stata riscritta come problema **ricorrente** invece che come lista di file:
  finché esistono file senza BOM, ogni commento accentato la riapre.
- ✅ **Licenze audio**: la build pubblica v0.19.2 non contiene audio non royalty-free.
  Il rischio non si è materializzato, ma da qui in avanti ogni clip nuova è pubblicata
  con la prima build che la contiene. `_RawAudio\Crowd` esiste ma il lavoro audio non è
  iniziato: non è materiale in lavorazione.
- 🟢 Confermato ancora aperto nel codice: `PoliceAI` a distanza 1 fa `if (atk <= def)
  break;` e non sa che dall'08/08 la carica spinge sempre.
- ✅ **Le tre domande di playtest hanno una risposta** (vedi le sezioni Panico e Spinta):
  il Coro va bene per ora e la strada è dividerlo in tre; l'arresto per schiacciamento è
  raro ed è giusto, perché il danno vero della spinta sono Coesione e tempo; l'obiettivo
  che fa muro **non è stato validato, è inerte** — la mappa attuale non permette che si
  presenti.
  ⚠ Nota di metodo che vale oltre questo caso: **"in playtest non è mai successo" non
  significa "la regola è a posto"**. Va sempre chiesto se la regola non ha scattato
  perché è buona o perché il livello non le ha dato occasione. Le due cose si scrivono
  uguali nel documento e significano l'opposto.

## Changelog sessione 33 (10/08/26) — passata di leggibilità
Il gioco comincia a raccontare quello che fa. Nessuna regola nuova: solo feedback.

- 🟢 **`UnitStatusView`**, componente nuovo: tinta di stato, tremore da panico, lampo da
  danno. Il tremore è stato **spostato fuori da `UnitMovement`**, dove stava solo perché
  il riferimento a `_graphicsTransform` era comodo. `UnitMovement` scende da 330 a 265
  righe e torna a occuparsi di una cosa sola.
- 🟢 **Riga di stato nel pannello unità**: `PANICKED — N turn(s)` e `SEATED`. Chiude anche
  una voce vecchia: il `+5` da seduto era invisibile, la Difesa mostrava un numero più alto
  senza dire perché.
- 🟢 **Lampo rosso al colpo**, agganciato ai tre momenti d'impatto reali invece che a
  `LoseMorale`. Vedi la sezione "Animazione e feedback visivo".
- 🟢 **`ExecuteSitStand` e `ExecuteBarricade` chiamano `RefreshBoardState`**. Era
  un'omissione "solo architetturale" da giorni: con la tinta è diventata visibile, perché
  sedersi non aggiornava niente.
- 🔴 **Un campo non assegnato sul pannello polizia ha bloccato il turno della polizia.**
  Vedi la sezione "Il pannello unità esiste in DUE copie".
- 📖 **Chiarita definitivamente la questione `transform.root`**: `_unitsDict` mappa
  l'unità sul figlio `Logic`, quindi `transform.root` è corretto e `transform.position`
  romperebbe il movimento. Due revisori esterni avevano proposto il contrario.

**Due lezioni:**

1. **Il punto unico della logica non è il punto unico della presentazione.** Il lampo su
   `LoseMorale` era architetturalmente elegante e visivamente sbagliato: la logica risolve
   al clic, l'animazione dura mezzo secondo. Quando l'effetto è visivo, il momento giusto
   lo decide l'animazione, e ogni animazione ha il suo.
2. **Un componente eredita le responsabilità dei riferimenti che possiede.** Il tremore e
   la tinta erano finiti in `UnitMovement` perché lì c'era `_graphicsTransform`. Nessuno
   l'aveva deciso: è successo. È così che nascono i god script, un metodo alla volta.

## Changelog sessione 32 (08/08/26, sera) — passata di pulizia
Nessuna feature nuova: solo tolto, spostato e accentrato. Il gioco si comporta identico
prima e dopo, ed è la condizione che rendeva questo lavoro sicuro.

- 🟢 **Conversione coordinate accentrata** in `HexGrid.GridToWorld` / `WorldToGrid`.
  Diciassette punti di chiamata, otto file. Chiude il bug di `UnitsRenderer.UpdateView`,
  che era l'unico a non sommare l'offset della griglia.
- 🟢 **Codice morto rimosso**: `UnitMovement.StopMovement` e `StopEveryMovement`,
  `HexGrid.IsCellWalkable` (duplicato peggiore di `TacticalQuery.IsCellAvailable`), i
  quattro campi Bump di `MovementSettingsSO`, il null-check morto in `PathFinder`
  (`HexCoordinates` è uno struct: quel confronto era sempre vero).
- 🟢 **Visibilità ristretta**: `Arrest`/`Disperse` e `PoliceAI.FoundNearestSpezzone` sono
  privati. Il primo conta: adesso **non è più scrivibile** un codice che tolga un'unità
  dal gioco scavalcando `RemoveFromBoard`, che è il punto unico dove si decide fra
  arresto e dispersione.
- 🟢 **`_hitReactionDistance` finalmente serializzato** nell'asset (era nel codice da
  settimane senza essere mai stato scritto su disco).
- 📖 **Distinzione stabilita fra residuo e riservato.** Codice che nessuno chiama e che
  non serve a niente si cancella; codice che nessuno chiama ma che serve a qualcosa di
  previsto si tiene **e si commenta**, altrimenti alla passata dopo qualcuno propone di
  nuovo di cancellarlo. Commentati come riservati: `HexCell.RemoveBarricade` (servirà
  quando la polizia potrà rimuovere barricate) e i tre campi Zona Rossa di `HexTypeSO`.
- 🔴 **Regressione di codifica**: `PathFinder.cs` è tornato ISO-8859-1 appena ci ho
  scritto un commento con due `è`. Visual Studio ricade sulla codepage di sistema sui
  file UTF-8 **senza BOM**. Risalvato con firma. Se scrivi commenti in italiano — e li
  scrivi sempre — i file devono avere il BOM, o il problema torna a ogni accento.

## Changelog sessione 31 (08/08/26) — query condivise, panico, carica ridisegnata
Giornata lunga: chiuso l'ultimo bug attivo, scritto il panico per intero, e cambiata una
regola di combattimento.

- 🟢 **B3+B7 chiusi — non ci sono più bug attivi noti.** `GetValidTargets` riceve unità e
  oggetto invece di coordinata e budget, e chiama gli stessi predicati degli esecutori
  (`GetSitStandCost`, `CanThrow`, `CanPlaceBarricade`). Chiuse gratis anche la voce 13
  dell'arretrato (barricata sugli obiettivi) e la gittata non verificata in `ExecuteThrow`.
- 🟢 **`InputHandler.DescribeInvalidTarget`**: l'alert dice il motivo vero invece di
  "not valid Target". Nata perché il fix ha spostato il rifiuto **prima** dell'esecutore,
  che i messaggi precisi ce li aveva.
- 🟢 **Panico completo**: stato, tremore DOTween, soppressione aure, propagazione BFS,
  aggancio alla carica, decremento per parte, cura col Coro. Manca la riga nel pannello.
- 🟢 **Sfogo laterale ridisegnato**: adesso può scartare **chiunque** nella colonna
  compressa, non solo il difensore. Chi scarta libera la sua cella e la fila davanti
  arretra. L'arresto per schiacciamento è diventato raro.
- 🟢 **La carica non confronta più Atk e Def.** Chi la subisce viene spinto, punto —
  tranne il seduto. Chiude gli Operai imbattibili, rende coerente il panico, e fa
  **evaporare la decisione E1** sulle aure calcolate dalla cella d'arrivo, che era il
  prerequisito del panico da tre giorni.
- 🔴 **Due regressioni introdotte dal refactor e trovate dal triple check**, entrambe
  dovute allo stesso fatto: il renderer ha ora una **copia** di `_selectedItem` e
  `_currentAction`. Vedi la sezione dedicata in PARTE 1.

- 🔴 **Due difetti nel panico, trovati dalla revisione della sera e corretti subito**:
  metà della regola sulle aure non era applicata (si riceveva ancora Atk/Def), e
  l'origine dell'onda si appoggiava a `_positionCell` non azzerato. Vedi la sezione
  "Panico" in PARTE 1.

**Quattro lezioni:**

1. **Un refactor che rende una query più precisa sposta i problemi a monte.** Chiudendo
   la divergenza, il rifiuto è passato dall'esecutore all'input — e con esso è sparito il
   messaggio giusto. Quando una decisione si sposta, va guardato **cosa si portava dietro**.
2. **Un cambio di design può cancellare una decisione aperta invece di risolverla.** E1
   era ferma da tre giorni e bloccava il panico; togliendo il confronto dalla carica non
   è stata decisa, ha smesso di esistere. Vale la pena chiedersi, davanti a una decisione
   incastrata, se la domanda sia ancora necessaria.
3. **Una regola con due versi va cercata in entrambi.** "Non dà e non riceve aure" era
   implementata solo per il *dare*. Il *ricevere* passava da un'altra strada
   (`CombatResolver`, che legge le aure al volo) e nessuno l'aveva percorsa. Il difetto
   era invisibile perché l'effetto vistoso — il Morale — funzionava.
4. **Non costruire sopra un bug che hai già deciso di demolire.** L'origine dell'onda
   leggeva `_positionCell` di un'unità rimossa, che è popolato solo per via di un difetto
   noto. Correggere quel difetto avrebbe rotto il panico in un punto lontanissimo.
   È la seconda volta che `_positionCell` non azzerato fa da appoggio a codice nuovo:
   la prima fu la resurrezione delle unità nella spinta, il 05/08.

## Changelog sessione 30 (06/08/26, sera) — passata di fix e spinta laterale
Stessa giornata della 29, ma qui il codice è stato toccato.

- 🟢 **Chiusi tutti i bug attivi tranne uno.** Input bloccato durante il turno polizia
  (`CanAcceptPlayerInput`), `EndTurn` che si ferma a partita finita, `TryOccupy` che
  controlla le barricate, `MoveCoroutine` che usa il ritorno di `SetPosition`, la guardia
  invertita in `UnitsSetup`, il pattern `_isValid` su `SelectionOutline`/`InputHandler`/
  `AudioManager`/`OrderPreviewRenderer`, i tre `WaitUntil` con fail-safe,
  `RefreshObjectiveCells` spostata in `Start`, il doppio `OnActionComplete` in
  `ConfirmMovement`, i sei canali evento di combattimento collegati in scena, i cinque
  file risalvati in UTF-8, log di rumore rimossi e messaggi tradotti in inglese.
  Resta aperta la sola divergenza highlight/esecuzione su SitStand, Throw e Barricade.
- 🟢 **Sbandamento laterale** (`TryStepAside` + `CountAdjacentAllies`), più la guardia di
  adiacenza in cima a `ResolvePushOrRemove`. Vedi la sezione dedicata.
- 🟢 **`ResolveOnce()` in `ExecuteCharge`**: il timeout da solo lasciava la carica pagata
  e non risolta.
- 📖 **Seconda revisione incrociata** sul dump v2 (`DISSENSO_SourceDump_2026-08-06_v2.md`).
  Entrambi i revisori esterni **non hanno trovato bug in `TryStepAside`**. L'unico difetto
  vero uscito è il timeout della carica.
- 🔴 **Correzioni al documento**: `UnitStatusView` non esiste; il panico **non** è uno stato
  di `UnitsStatus`; `HexTypeSO` ha tre campi dichiarati e mai letti (`IsRedZone`,
  `ModifierA`, `ModifierB`) — la Zona Rossa non è implementata come *regola*, ma il campo
  dati c'è da mesi e il documento diceva "non esiste".
- 📖 **Design del panico rivisto** (vedi cap. 17.4 e la sezione "Panico" qui sopra): il
  danno esce dalla propagazione, il gradiente passa alla durata, il "punto unico" per il
  decremento era sbagliato di un turno.

**Due lezioni di metodo:**

1. **Ero io a sbagliare sulla Zona Rossa.** Avevo dato per inventata una proprietà
   (`IsRedZone`) che DeepSeek aveva citato correttamente, perché mi ero fidato di questo
   documento invece di aprire `HexTypeSO`. Terza volta in due giorni che il documento
   invecchia più in fretta del codice, e la prima in cui stavo per correggere qualcuno
   avendo torto.
2. **Un fix contro un blocco può riaprire un bug peggiore.** Il timeout sui `WaitUntil`
   sbloccava il gioco ma lasciava la carica irrisolta, e senza il flag `resolved` una
   callback in ritardo avrebbe fatto girare `PushResolution` due volte. Ogni volta che si
   aggiunge una via d'uscita a un'attesa, va chiesto **cosa succede se quello che stavi
   aspettando arriva comunque, dopo.**

## Changelog sessione 29 (06/08/26) — triple check, nessuna riga di codice toccata
Sessione dal portatile. Working tree pulito su `518220952`; niente è stato modificato
nel codice, solo letto e documentato.

- 📖 **Dump completo dei 67 script** in `D:\GDDRIOT\DISSENSO_SourceDump_2026-08-06.md`,
  con contesto, regole architetturali e lista "già noto" in testa. Serve per far
  rileggere il progetto da revisori esterni senza doverglielo rispiegare ogni volta.
- 📖 **Revisione incrociata a tre** (Claude sul repo e sulla scena, ChatGPT e DeepSeek
  sul dump). Risultato in `D:\GDDRIOT\FIXLIST_2026-08-06.md`: 4 bug attivi,
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
