# 0005 — Integrazione HexGridController e click libero

**Stato**: da fare · **Commit di riferimento**: —

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
- [ ] `dotnet test Tools/RulesHarness` verde (se tocca Rules)
- [ ] Nessuna regola nuova inventata (TODO R-xxx dove serve)
- [ ] Numeri letti da `RulesConfig`

## Report
(append-only)
