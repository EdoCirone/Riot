# Fonti audio — DISSENSO

Registro interno. **Non sono i crediti del gioco**: questo file serve a noi per sapere
da dove viene ogni file e cosa possiamo farci. I crediti mostrati al giocatore sono
un'altra cosa e stanno nel pannello Credits.

Regola: ogni volta che entra un file audio nel progetto, si aggiunge una riga QUI,
subito. Ricostruire la provenienza a posteriori con quaranta clip è un pomeriggio perso.

---

## ✅ Nessun file a licenza problematica

Il 3/8/2026 **tutti i file ElevenLabs sono stati cancellati dal progetto** (8 wav in
`Assets` + 11 mp3 sorgente in `_RawAudio`). Motivo: il piano Free di ElevenLabs non
include licenza commerciale e richiede l'attribuzione nel titolo del contenuto
pubblicato — condizioni incompatibili con una build distribuita, anche gratuita.

**Oggi il progetto è interamente Mixkit.** Se in futuro rientra materiale da altre
fonti, va aggiunta qui una sezione "da sostituire" prima che finisca in una build.

Nota di metodo: i file erano stati nominati per **uso** (`SFX_StartTurn_01`) e non
per **provenienza**, e dopo qualche ora nessuno ricordava più quali fossero di quale
fonte. Se un domani entra materiale con vincoli, dargli un prefisso visibile
(es. `TMP_`) è più efficace di una riga in questo file: il promemoria deve stare
dove si lavora, non dove si documenta.

---

## Utilizzabili nella build

| File | Fonte | Licenza | Attribuzione | Data |
|---|---|---|---|---|
| `Crowd/Ambience/AMB_CrowdAngry.wav` | Mixkit (`angry-male-crowd-ambience-458`) | Mixkit Free License | Non richiesta (la mettiamo per scelta) | ago 2026 |
| `Crowd/Ambience/AMB_CrowdRiot.wav` | Mixkit (`rioting-crowd-376`) | Mixkit Free License | idem | ago 2026 |
| `Crowd/Ambience/AMB_RiotWindowsSiren.wav` | Mixkit (`rioting-crowd-breaking-windows-and-police-siren-445`) | Mixkit Free License | idem | ago 2026 |
| `Crowd/Chant/SFX_Chant_Sports.wav` | Mixkit (`chanting-sports-crowd-433`) | Mixkit Free License | idem | ago 2026 |
| `Crowd/oneMoreRound/SFX_StartTurn_01..03_riot.wav` | Mixkit (`rioting-crowd-376`), ritagli a 0.50s / 14.52s / 28.54s | Mixkit Free License | idem | ago 2026 |
| `Crowd/oneMoreRound/SFX_StartTurn_04..06_angry.wav` | Mixkit (`angry-male-crowd-ambience-458`), ritagli a 0.50s / 10.74s / 20.99s | Mixkit Free License | idem | ago 2026 |
| `Crowd/Chant/SFX_Chant_Revolution.wav` | Mixkit (`male-crowd-chanting-revolution-440`) | Mixkit Free License | idem | ago 2026 |
| Musica MainMenu — "Furious" di Fass | Uppbeat — uppbeat.io | Uppbeat free tier | **OBBLIGATORIA**, nella forma esatta indicata sotto | — |

### Attribuzione Uppbeat — forma vincolata

Va riportata così, hashtag e URL compresi. Non è una convenzione, è un requisito
del piano gratuito:

```
Music from #Uppbeat (free for Creators!)
https://uppbeat.io/t/fass/furious
```

---

## Note operative

- **Conserva una copia datata dei testi di licenza.** Le condizioni cambiano, e
  "nel 2026 diceva così" è dimostrabile solo se ne hai salvato un PDF. Vale per
  Mixkit, Uppbeat ed ElevenLabs.
- **"Placeholder" non è una categoria che esiste nel diritto d'autore.** Se un file
  è in una build pubblica, la sua licenza si applica — che tu lo consideri
  provvisorio o no. Per questo la tabella in cima esiste.
- **freesound non è tutto CC0**: è un misto CC0 / CC-BY / CC-BY-NC. Se si attinge
  da lì, filtrare esplicitamente per CC0 e annotare autore e URL per ogni clip.
- **Normalizzazione — due bersagli diversi, non uno.**
  - **Effetti one-shot** (inizio turno, cori, colpi): **−16 LUFS**, TP −1,5 dBFS.
    Devono farsi sentire sopra il resto.
  - **Ambienze in loop** (folla di fondo, rivolta lontana): **−23 LUFS**, TP −2 dBFS.
    Stanno sotto tutto, sennò coprono gli effetti che portano informazione.
    Normalizzare un'ambienza al livello di un effetto è l'errore classico: il gioco
    diventa un muro di rumore in cui non distingui più niente.
  - Comando one-shot: `ffmpeg -i in.wav -af loudnorm=I=-16:TP=-1.5:LRA=11 -ar 44100 out.wav`
  - Per le clip **lunghe** (oltre ~10 s) serve la **doppia passata**: la prima misura
    (`print_format=json`), la seconda applica i valori misurati. In passata singola
    `loudnorm` lavora in tempo reale e il livello sbanda lungo la clip.
- **Cartelle**: `Crowd/oneMoreRound/` = cue di inizio turno; `Crowd/Chant/` = cori;
  `Crowd/Ambience/` = letti sonori in loop. I sorgenti grezzi stanno in
  `D:\Riot\_RawAudio\`, **fuori da `Assets`**, così Unity non li importa e non
  finiscono per sbaglio dentro un `SFXSO` al posto delle versioni normalizzate.
- **Le folle restano stereo.** La larghezza fa parte di ciò che le rende folle.
  Il `Force To Mono` va bene per click e impatti, non per i cori.
