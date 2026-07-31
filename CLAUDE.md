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
- Assets/_Recovery/0.unity e 0 (1).unity — copie create dal recupero dell'Editor
  dopo un crash e fuori dalle build settings. `0 (1).unity` è identica a LVLTest;
  `0.unity` è una versione precedente. Non sono scene di progetto e possono
  essere rimosse insieme ai rispettivi file `.meta`.

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
- "Zero singleton statici" era la regola, ma NON è più vera nel codice:
  `GameManager.instance` è un `public static` (vedi bug noti).
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

## Audio (AudioManager + SFXSO)
- `AudioManager` (DontDestroyOnLoad) tiene un `AudioMixer` con tre parametri
  esposti: `VolumeMaster`, `VolumeMusic`, `VolumeSFX`. Due AudioSource distinti
  per musica e SFX. Volumi salvati in PlayerPrefs con gli stessi nomi.
- `SFXSO` mappa un `GameEventSO` a un `AudioClip`. In `OnEnable`, `AudioManager`
  sottoscrive un handler per ogni elemento di `_sfxevents`; in `OnDisable`
  annulla le sottoscrizioni usando `_sfxHandlers`.
- `EventMusicSO` è l'event channel che veicola un AudioClip da riprodurre in loop.
- I volumi lineari sono convertiti correttamente in decibel con
  `Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f`.

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

## Scontro (CombatResolver — REALE)
- **Deterministico, nessun dado.** Confronto secco:
  - Atk attaccante > Def difensore → Win
  - Atk attaccante < Def difensore → Lose
  - uguali → Par
- Nessun modificatore (no Coesione, no fasce, no malus distacco, no Zona Rossa).

## Azioni e loro effetti
- **Scontro (Skirmish)**: richiede distanza esattamente 1. Costa 1 PA. Non sposta
  nessuno, intacca solo il Morale. Win → difensore -1 Morale; Lose → attaccante
  -1; Par → entrambi -1.
- **Carica (Charge)**: richiede distanza esattamente 3 IN LINEA RETTA PURA
  (HexDirectionFinder), con le 2 celle intermedie libere. Costa 4 PA. L'attaccante
  si sposta adiacente al difensore, poi si risolve la spinta:
  Win → difensore spinto di 1 oltre; se la cella è occupata cerca una laterale
  comune; se nessuna → difensore Disperse. Lose → simmetrico sull'attaccante.
  Par → nessuno si muove.
- **Muovi+attacca**: per police a distanza diversa da 1 e 3, lo spezzone si
  avvicina (FindBestAdjacentCell + A*) e poi fa scontro. Richiede PA per il
  percorso + 1. Sfocia SOLO in scontro, mai in carica.
- Spinta: CalculatePushDestination proietta oltre il difensore nella direzione
  attaccante→difensore.
- **Coro (Chant)**: costa 3 PA. +1 Morale a chi lo lancia e a ogni SpezzoneRuntime
  vivo nelle 6 celle adiacenti. Nessun effetto sulla polizia.
- **Sedersi/Alzarsi (SitStand)**: sedersi costa 1 PA, alzarsi 2. Da seduto
  `SpezzoneRuntime.Def` vale `Def + 5` (il bonus vive nell'override di Def, non
  in AbstractUnitsRunTime: PoliceRuntime non lo ha).
- **Lancio (Throw) e Barricata**: `ExecuteThrow` / `ExecuteBarricade` esistono in
  TurnManager, gittate e costi NON ancora verificati riga per riga — da leggere
  in una sessione dedicata prima di documentarli qui.

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
- **TurnManager.CanCharge è DEAD CODE**: nessun chiamante nell'intero progetto
  (grep 27/07/26). Rimpiazzata da GetAttackOption. Sicura da cancellare.
- **OrderPreviewRenderer._turnManager è un campo inutile**: dopo il passaggio a
  GetAttackOption il riferimento serve solo a comparire in un null-check di guardia
  (riga 32). Da rimuovere insieme al null-check.
- **GameManager.instance è un singleton statico pubblico**: viola la regola
  architetturale "zero singleton".
- **Campi Bump in MovementSettingsSO sono dead code** (ChargeBumpDistance/Duration,
  SkirmishBumpDistance/Duration): definiti, esposti, mai letti da nessuno.
  NON è perché manchi l'animazione di ricezione colpo — quella ESISTE:
  `UnitMovement.PlayHitReaction`, chiamata da `ExecuteSkirmish` via il callback
  `onImpact`, e usa campi diversi (`HitReactionDistance`, `RecoilDuration`,
  `SkirmishAtkDuration`). I campi Bump sono residui di una nomenclatura
  precedente. Verificato 27/07/26.
- **PathFinder è MonoBehaviour ma senza stato**: potrebbe/dovrebbe essere classe
  statica come CombatResolver/TacticalQuery. Incoerenza da sanare.
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

# DA FARE (concordato)
1. Animazione ricezione colpo del difensore: FATTA per lo scontro
   (`PlayHitReaction` via `onImpact` in ExecuteSkirmish). MANCANO la versione per
   la carica e la distinzione win/lose. Verificato 27/07/26.
2. Animazione scontro polizia (stessa logica attaccante).
3. Riprodurre e fixare il bug muovi+attacca combinato (catturare quale dei tre
   messaggi di ConfirmAttack esce: cella adiacente / percorso / PA insufficienti).

# Dipendenze Unity
- com.unity.feature.2d 2.0.1
- com.unity.render-pipelines.universal 17.0.3
- com.unity.inputsystem 1.13.0
- DOTween (animazione)
