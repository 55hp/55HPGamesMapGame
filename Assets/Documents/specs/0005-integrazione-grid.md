# 0005 — Integrazione HexGridController e click libero

**Stato**: fatto, verificato in Play mode · **Commit di riferimento**: —

## Obiettivo
Collegare le regole alla griglia: click libero, rimozione di adiacenza e Cibo.

## Contratto (Athena)
R-002, R-003, R-005, R-027. `IsClickable` = coperta e non Vuota (o forziere riapribile). `TryRevealTile` chiama `RulesEngine.Reveal`. Rimuovere Food/Flee/RecomputeReachability dai percorsi toccati.

## Lato codice (Claude Code)
Adapter eventi → EventBus. `ContentKindMap`. Non toccare Core.

## Lato Editor (Bezi) — checklist
- Aprire la scena di gioco e verificare i riferimenti rotti dopo la rimozione di Food (se ce ne sono).

## Per Franci
- Lancia in Unity i test EditMode indicati nel report.
- Commit/push quando i test sono verdi.

## Verifica
- [ ] `Tools/RulesHarness/run.sh` verde (se tocca Rules)
- [ ] Nessuna regola nuova inventata (TODO R-xxx dove serve)
- [ ] Numeri letti da `RulesConfig`

## Report
(append-only)

### Athena — 2026-11-03 — branch `athena/v2-impl`
- `HexGridController` riscritto come adapter del `RulesEngine`: stessa interfaccia verso viste/camera/tap; click libero (R-002); niente Cibo/Fuga/reachability. Metodi v1 lasciati come stub `[Obsolete]` per non rompere `UIPopup_Encounter`/`UIPopup_Shop` (rimozione: 0012).
- `IGameContextService` specchiato (Lives=HP, Score=Monete, chiavi argento/oro su DL4/DL5) per HUD e pagina risultati legacy.
- `TileType` esteso in coda (D-022): MiniBoss 22, Smith 23, Villagers 24, ChestSilver 25, ChestGold 26, Empty 27, Start 28, Objective 29. `ContentKindMap` unico punto di traduzione.
- Vista: il bordo di mascheramento resta visibile su tutte le tile coperte (nel prefab bordo e indicatore di cliccabilità sono lo stesso oggetto).
