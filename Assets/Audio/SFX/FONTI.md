# Fonti audio — DISSENSO

Registro interno. **Non sono i crediti del gioco**: questo file serve a noi per sapere
da dove viene ogni file e cosa possiamo farci. I crediti mostrati al giocatore sono
un'altra cosa e stanno nel pannello Credits.

Regola: ogni volta che entra un file audio nel progetto, si aggiunge una riga QUI,
subito. Ricostruire la provenienza a posteriori con quaranta clip è un pomeriggio perso.

---

## ⚠ Da sostituire prima di qualunque build pubblica

| File | Fonte | Problema |
|---|---|---|
| `Crowd/oneMoreRound/SFX_StartTurn_01..06.wav` | ElevenLabs (piano **Free**) | Il piano gratuito **non include licenza commerciale**. Verificato il 3/8/2026. Usabili solo come segnaposto interni. |
| `Crowd/SFX_Long_Chant_01..02.wav` | ElevenLabs (piano **Free**) | Idem. Candidati per il Coro, ma da rigenerare da altra fonte. |

Opzioni per sostituirli: rigenerare da Mixkit (sezioni Human / Warfare), oppure
passare al piano Starter di ElevenLabs, che include l'uso commerciale.

---

## Utilizzabili nella build

| File | Fonte | Licenza | Attribuzione | Data |
|---|---|---|---|---|
| `Crowd/Ambience/AMB_CrowdAngry.wav` | Mixkit (`angry-male-crowd-ambience-458`) | Mixkit Free License | Non richiesta (la mettiamo per scelta) | ago 2026 |
| `Crowd/Ambience/AMB_CrowdRiot.wav` | Mixkit (`rioting-crowd-376`) | Mixkit Free License | idem | ago 2026 |
| `Crowd/Ambience/AMB_RiotWindowsSiren.wav` | Mixkit (`rioting-crowd-breaking-windows-and-police-siren-445`) | Mixkit Free License | idem | ago 2026 |
| `Crowd/Chant/SFX_Chant_Sports.wav` | Mixkit (`chanting-sports-crowd-433`) | Mixkit Free License | idem | ago 2026 |
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
