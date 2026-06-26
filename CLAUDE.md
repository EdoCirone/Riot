# CLAUDE.md

## Progetto
DISSENSO (ex RIOT) — gioco tattico a turni 2D in Unity 6000.4.5f1 (URP).
Il giocatore comanda un corteo politico (spezzoni) su una griglia esagonale flat-top contro forze di polizia.
Lingua team: italiano. Commit e commenti in italiano. Nomi variabili/classi in inglese.

## Unity Version
6000.4.5f1 — non aggiornare mai senza istruzione esplicita.

## Scena principale
Assets/Scenes/Main.unity

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
- Zero singleton statici. Comunicazione via event channel SO (pattern Ryan Hipple):
  i sistemi si sottoscrivono direttamente agli asset SO.
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
obiettivo) / TurnManager (esecuzione azioni, ciclo turni). PoliceAI orchestra
il turno polizia. (RunManager citato in design ma non presente tra i file core
attuali — verificare se esiste.)

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

## Unità (stat REALI)
`AbstractUnitsRunTime` espone SOLO quattro stat:
- **Atk**, **Def** (da SpezzoneSO/PoliceSO), **Morale**, **ActionPoints** (+ i max).
- Stato: Alive / Disperse (enum UnitsStatus).
- Morale a 0 → Disperse (l'unità vacate la cella e sparisce dalla view).
- SpezzoneRuntime e PoliceRuntime differiscono solo per la fonte SO di Atk/Def
  e il prefab grafico.

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
  Il gioco usa esecuzione immediata.
  NB: esiste ancora DEAD CODE di questo sistema sul disco, mai referenziato:
  Assets/Script/Enum/TurnPhases.cs (enum Decision/Resolution/EndTurn),
  Assets/Script/Units/Data/Old/AttackOrder.cs e MovementOrder.cs.
  DA RIMUOVERE (vedi bug noti).
- ZOC (zona di controllo: chi entra in cella adiacente si ferma e attiva scontro):
  non implementata.
- Stat Reattività / Aggressività / Coesione: non esistono nel codice.
- Modificatori di scontro (fasce Coesione, malus distacco, Zona Rossa): non esistono.
- 6 gruppi politici tipizzati (Pacifisti, Operai, Studenti, Anarchici, Black Bloc,
  Movimento): non esistono come tipi; ci sono SpezzoneSO/PoliceSO generici.
- Casualità nello scontro (dado): non esiste; lo scontro è deterministico.

---

# PARTE 3 — V0.2 (in progettazione)

Da definire in sessione di design. Nucleo previsto:
- **Inventario** per unità (sistema di stato per-unità: cosa porta, come si spende,
  come la UI lo mostra). È il sistema portante; lancio e barricata ne sono usi.
- **Lancio**: azione a distanza (da definire: gittata, costo PA, effetto).
- **Barricata**: elemento difensivo/statico (da definire).
NB: progettare il modello di inventario PRIMA e in modo generale, non attorno ai
due soli item, per evitare un inventario che regge solo lancio e barricata.

---

# BUG NOTI / DEBITO TECNICO

- **Muovi+attacca combinato subottimale**: il comando combinato può rifiutare per
  costo-path quando FindBestAdjacentCell sceglie una cella adiacente per distanza
  diretta e non per costo di percorso reale (l'A* gira intorno agli ostacoli).
  Workaround: scomporre a mano (avvicinati, poi attacca). Da fixare facendo
  ordinare le adiacenti per costo-path, ma tocca anche l'highlight.
- **Divergenza highlight/esecuzione su distanza 3 non allineata** (trovato dal check):
  l'highlight (CanMoveAndSkirmish) NON filtra il caso distanza-3, quindi se un vicino
  del bersaglio è raggiungibile in BFS colora la cella in rosso (muovi+attacca). Ma
  ConfirmAttack su distanza == 3 dispatcha SEMPRE a ExecuteCharge senza fallback: se
  la carica fallisce per non-allineamento, esce con "Carica non valida" e NON tenta
  il muovi+attacca. Risultato: cella rossa che promette, click che non esegue.
  Stesso buco in PoliceAI (se ExecuteCharge torna false il loop esce senza avvicinarsi).
- **Nota comune alle due divergenze sopra**: entrambe nascono dal fatto che highlight
  ed esecuzione decidono "che azione è possibile" con logiche separate. La query
  d'attacco unificata (vedi sotto) le chiuderebbe ENTRAMBE in un colpo.
- **Migrazione TacticalQuery a metà**: spostati IsCellAvailable, GetReachable,
  HasChargeRoom, FindBestAdjacentCell. CanCharge resta proxy in TurnManager
  (adattatore unità→coordinate, accettabile). NON fatta la query d'attacco
  unificata (GetAttackOption): HighlightAttackable e ConfirmAttack decidono ancora
  separatamente cosa è attaccabile — duplicazione residua da chiudere.
- **OrderPreviewRenderer dipende da TurnManager** solo per CanCharge: asimmetria
  (movimento via TacticalQuery diretto, carica via TurnManager). La query d'attacco
  unificata la risolverebbe.
- **Campi Bump in MovementSettingsSO** (4 campi: ChargeBump*, SkirmishBump*) da
  RIMUOVERE finché non si implementa l'animazione di ricezione colpo del difensore.
- **PathFinder è MonoBehaviour ma senza stato**: potrebbe/dovrebbe essere classe
  statica come CombatResolver/TacticalQuery. Incoerenza da sanare.
- **Dead code da rimuovere** (confermato non referenziato dal check):
  Assets/Script/Enum/TurnPhases.cs, Assets/Script/Units/Data/Old/AttackOrder.cs,
  Assets/Script/Units/Data/Old/MovementOrder.cs. Residui del sistema a fasi
  abbandonato. Sicuri da cancellare (grep: zero riferimenti esterni).
- **Modello d'attacco a distanze fisse**: scontro solo a distanza 1, carica solo a
  distanza 3 in linea retta pura. Distanza 2 e distanza-3-non-allineata cadono in
  muovi+attacca. Comportamento VOLUTO, non bug — documentato per chiarezza.

# DA FARE (concordato)
1. Animazione bump difensore (win/lose separati) per scontro e carica.
2. Animazione scontro polizia (stessa logica attaccante).
3. Riprodurre e fixare il bug muovi+attacca combinato (catturare quale dei tre
   messaggi di ConfirmAttack esce: cella adiacente / percorso / PA insufficienti).

# Dipendenze Unity
- com.unity.feature.2d 2.0.1
- com.unity.render-pipelines.universal 17.0.3
- com.unity.inputsystem 1.13.0
- DOTween (animazione)
