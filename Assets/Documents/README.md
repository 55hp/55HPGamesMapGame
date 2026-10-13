# MapGame v2 — README di progetto

Puzzle-roguelike su griglia esagonale 10×10, mobile. Unity 2022.3.62f3 + URP. Base: "55HP Games Unity Mobile Template". Branch `v2` da `develop` @ `fdfe080`.

Documentazione: `tech/00_INDICE.md`. Regole: `tech/02_regole.md`, `tech/03_contenuti.md`. Scelte provvisorie da rivedere: `tech/08_decisioni-default.md`. Spec: `specs/`. Piano: `piano/`.

## Il gioco in breve

Il giocatore scopre tile coperte in qualsiasi ordine. Ogni tile ha un bordo colorato (livello di mascheramento 1–6) e nasconde mostri, trappole, arbusti, forzieri, NPC o il Boss. Un solo parametro, l'Attacco, decide gli scambi: Attacco ≥ Difesa uccide e premia, altrimenti il bersaglio fugge e il giocatore subisce la differenza. Gli indizi narrativi dicono quante tile di un tipo ci sono su una riga o colonna (nanogram). Si vince uccidendo il Boss (Difesa 8).

## Stato (aggiornato da Claude Code)

| Spec | Titolo | Stato | Commit |
|---|---|---|---|
| 0001 | Branch, assembly Rules, harness | da fare | — |
| 0002 | RunState, equipaggiamento, scambio, XP | da fare | — |
| 0003 | Modello contenuti e RulesConfig | da fare | — |
| 0004 | Generatore a censimento | da fare | — |
| 0005 | Integrazione HexGridController, click libero | da fare | — |
| 0006 | UI incontro e HUD v2 | da fare | — |
| 0007 | Level up | da fare | — |
| 0008 | NPC | da fare | — |
| 0009 | Indizi | da fare | — |
| 0010 | Forzieri, chiavi, mini-boss | da fare | — |
| 0011 | Missione Caccia | da fare | — |
| 0012 | Pulizia legacy e bot di bilanciamento | da fare | — |

Milestone: `piano/01_roadmap.md`. Rischi: `piano/02_rischi.md`. Workflow: `piano/03_workflow-e-branch.md`.

## Come lavoriamo

Franci (design, commit/push, playtest) → Athena (documenti, prompt) → Claude Code (C#, test) → Bezi (scene, prefab, asset). Dettagli: `../../CLAUDE.md` e `Bezi_Rules.md`.

## Ambiente

Unity Hub, Editor `2022.3.62f3`. Test regole: `dotnet test Tools/RulesHarness` (dopo spec 0001). Test Unity: Test Runner → EditMode.
