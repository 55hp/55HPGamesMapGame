# MapGame v2 — README di progetto

Puzzle-roguelike su griglia esagonale 10×10, mobile. Unity 2022.3.62f3 + URP. Base: "55HP Games Unity Mobile Template". Branch `v2` da `develop` @ `fdfe080`.

Documentazione: `tech/00_INDICE.md`. Regole: `tech/02_regole.md`, `tech/03_contenuti.md`. Scelte provvisorie da rivedere: `tech/08_decisioni-default.md`. Spec: `specs/`. Piano: `piano/`.

## Il gioco in breve

Il giocatore scopre tile coperte in qualsiasi ordine. Si gioca subito: Play da `00_Bootstrap` → Play nel menu (UI di prototipo IMGUI, spec 0006). Ogni tile ha un bordo colorato (livello di mascheramento 1–6) e nasconde mostri, trappole, arbusti, forzieri, NPC o il Boss. Un solo parametro, l'Attacco, decide gli scambi: Attacco ≥ Difesa uccide e premia, altrimenti il bersaglio fugge e il giocatore subisce la differenza. Gli indizi narrativi dicono quante tile di un tipo ci sono su una riga o colonna (nanogram). Si vince uccidendo il Boss (Difesa 8).

## Stato (aggiornato da Claude Code)

| Spec | Titolo | Stato | Commit |
|---|---|---|---|
| 0001 | Branch, assembly Rules, harness | fatto, verificato in Unity | athena/v2-impl |
| 0002 | RunState, equipaggiamento, scambio, XP | fatto | athena/v2-impl |
| 0003 | Modello contenuti e RulesConfig | fatto (asset opzionale) | athena/v2-impl |
| 0004 | Generatore a censimento | fatto | athena/v2-impl |
| 0005 | Integrazione HexGridController, click libero | fatto, verificato in Play | athena/v2-impl |
| 0006 | UI incontro e HUD v2 | UI di prototipo (IMGUI) | athena/v2-impl |
| 0007 | Level up | fatto | athena/v2-impl |
| 0008 | NPC | fatto | athena/v2-impl |
| 0009 | Indizi | fatto | athena/v2-impl |
| 0010 | Forzieri, chiavi, mini-boss | fatto | athena/v2-impl |
| 0011 | Missione Caccia | fatto | athena/v2-impl |
| 0012 | Pulizia legacy e bot di bilanciamento | bot fatto, pulizia da fare | athena/v2-impl |

Milestone: `piano/01_roadmap.md`. Rischi: `piano/02_rischi.md`. Workflow: `piano/03_workflow-e-branch.md`.

## Come lavoriamo

Franci (design, commit/push, playtest) → Athena (documenti, prompt) → Claude Code (C#, test) → Bezi (scene, prefab, asset). Dettagli: `../../CLAUDE.md` e `Bezi_Rules.md`.

## Ambiente

Unity Hub, Editor `2022.3.62f3`. Test regole: `Tools/RulesHarness/run.sh` (Linux, Mono) o `dotnet run --project Tools/RulesHarness` (Windows). Test Unity: Test Runner → EditMode.
