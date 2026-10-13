# 00 — Indice dei contratti tecnici

Fonte di verità: il repo, branch `v2` (da `develop` @ `fdfe080`). Notion non fa parte del flusso degli agenti.

| File | Contenuto | Proprietario |
|---|---|---|
| `01_architettura.md` | assembly, regole d'oro, stato, flusso, delta codice | Athena |
| `02_regole.md` | regole di gioco R-xxx (il GDD) | Athena/Franci |
| `03_contenuti.md` | censimento, NPC, oggetti | Athena/Franci |
| `04_mappa.md` | generazione e griglia | Athena |
| `05_indizi.md` | nanogram narrativo | Athena |
| `06_presentazione.md` | UI e HUD | Athena |
| `07_test-e-simulazione.md` | test e bot di bilanciamento | Athena |
| `08_decisioni-default.md` | registro delle scelte provvisorie | Athena → Franci |

Altro: `../README.md` (stato), `../Bezi_Rules.md`, `../specs/` (task), `../piano/` (roadmap, rischi, workflow), `../data/*.csv` (dati leggibili da macchina, devono coincidere con `03`).

## Regola di sincronia

Per le regole vince la documentazione; per lo stato vince il codice. Se il codice contraddice un documento, non si modifica il documento: domanda nel report. I CSV in `data/` sono derivati di `03`: se divergono, vale `03`.

## Registro modifiche

| Data | File | Modifica |
|---|---|---|
| 2026-10-13 | tutti | Prima stesura v2 (consolidamento GDD, default D-001..D-024) |
