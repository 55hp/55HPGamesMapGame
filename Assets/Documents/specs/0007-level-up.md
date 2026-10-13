# 0007 — Flusso di level up

**Stato**: da fare · **Commit di riferimento**: —

## Obiettivo
Decisione pendente con tre scelte e relativo pop-up.

## Contratto (Athena)
R-042, R-043. `LevelUpDecision` con opzioni legali; `Submit(optionId)`; più level up in una mossa si risolvono in sequenza.

## Lato codice (Claude Code)
Test: ogni opzione; cumulabilità; più livelli.

## Lato Editor (Bezi) — checklist
- Prefab `Popup_LevelUp` con tre bottoni.

## Per Franci
- Lancia in Unity i test EditMode indicati nel report.
- Commit/push quando i test sono verdi.

## Verifica
- [ ] `dotnet test Tools/RulesHarness` verde (se tocca Rules)
- [ ] Nessuna regola nuova inventata (TODO R-xxx dove serve)
- [ ] Numeri letti da `RulesConfig`

## Report
(append-only)
