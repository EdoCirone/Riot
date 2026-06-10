# CLAUDE.md

## Progetto
RIOT — gioco strategico a turni 2D in Unity 6000.4.5f1 (URP).
Il giocatore comanda un corteo politico su una griglia esagonale contro forze di polizia statiche.
Linguaggio del team: italiano.Commit messages e commenti in italiano
Nomi variabili e classi in inglese

## Unity Version
6000.4.5f1 — non aggiornare mai senza istruzione esplicita.

## Scena principale
Assets/Scenes/Main.unity

## Architettura — regole fondamentali
- ScriptableObject = dati statici (SpezzoneSO, GruppoPoliticoSO, ReggimentoSO)
- Classe Runtime = stato vivo della partita (SpezzoneRuntime, CorteoRuntime)
- MonoBehaviour = oggetti in scena e orchestrazione
- UI = visualizzazione e input, mai logica di gameplay
- Ogni script ha una responsabilità principale
- Zero singleton statici — comunicazione tramite EventChannelSO (observer pattern)

## Naming Convention
- Classi: PascalCase
- Campi privati serializzati: _camelCase
- Proprietà pubbliche: PascalCase
- Metodi: PascalCase, verbo chiaro
- Metodi che possono fallire: prefisso Try, restituiscono bool
- Eventi: prefisso On
- Metodi che lanciano eventi: prefisso Raise
- ScriptableObject: suffisso SO
- Classi runtime: suffisso Runtime
- UI: suffisso UI
- Manager solo per sistemi centrali

## Struttura cartelle
Assets/Script/
├── Core/       GameEventSO, TurnStateSO, TurnManager
├── Map/        HexCell, HexGridSO, HexPathfinder
├── Units/      GruppoPoliticoSO, ReggimentoSO, SpezzoneRuntime, CorteoRuntime
├── Combat/     ScontroPipeline, EsitoScontro
└── UI/         solo visualizzazione, zero logica gameplay

## Griglia
Esagonale. Coordinate axial (q, r). 6 direzioni. 
Euristica hex: max(abs(dq), abs(dr), abs(dq+dr)) / 2

## Unità
Spezzoni (corteo): Attacco, Difesa, Morale, Reattività
Reggimenti (polizia): Attacco, Difesa, Reattività, Aggressività
6 gruppi politici: Pacifisti, Operai, Studenti, Anarchici, Black Bloc, Movimento

## Sistema a turni
Fase Decisionale: il giocatore assegna ordini, niente si muove
Fase Risolutiva: esecuzione in ordine di Reattività, parità va alla polizia
ZOC: ogni unità controlla le 6 celle adiacenti, chi entra si ferma e attiva scontro

## Scontro
Formula: Attacco vs Difesa (deterministico, niente dado)
Attacco > Difesa → difensore spinto di 1 cella
Attacco = Difesa → stallo
Attacco < Difesa → attaccante respinto di 1 cella
Modificatore Coesione per fascia: >66 = +2 Difesa, 33-66 = +1, <33 = +0
Spezzone distaccato: -1 Difesa
Zona Rossa: magnete tattico (non modifica Difesa)

## V0.1 — scope
- Griglia esagonale statica hardcoded
- 3 spezzoni giocatore, 2 reggimenti statici
- Click per selezionare, click per assegnare movimento
- Fine Turno esegue movimenti in ordine Reattività
- ZOC triggera scontro con tiro dado e esito a schermo
- Nessuna condizione di vittoria, loop infinito
- Forme geometriche colorate, zero sprite

## Dipendenze Unity
- com.unity.feature.2d 2.0.1
- com.unity.render-pipelines.universal 17.0.3
- com.unity.inputsystem 1.13.0
