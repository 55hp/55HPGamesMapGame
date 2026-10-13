# 0002 — RunState, equipaggiamento, scambio, XP

**Stato**: da fare · **Commit di riferimento**: —

## Obiettivo
Implementare lo stato di run e la risoluzione di incontro, XP e progressione, tutto in Rules.

## Contratto (Athena)
R-020..R-025, R-029, R-031..R-036, R-040..R-044, R-038 (bonus monete a ogni guadagno), R-043. `RunState`, `EquipmentSlots`, `AttackCalculator`, `EncounterResolver.Resolve(runState, defense)` → `EncounterResult` (Won/Fled, danno, ricompensa). `XpService` con linee completate e livelli.

## Lato codice (Claude Code)
Tutto in `Assets/Game/Rules/`. Eventi di regola come tipi puri (`DamageTaken`, `XpGained`, `LevelUpAvailable`, …). I numeri da `RulesConfig` (stub finché 0003 non c'è).

## Lato Editor (Bezi) — checklist
- Nessuna.

## Per Franci
- Lancia in Unity i test EditMode indicati nel report.
- Commit/push quando i test sono verdi.

## Verifica
- [ ] `dotnet test Tools/RulesHarness` verde (se tocca Rules)
- [ ] Nessuna regola nuova inventata (TODO R-xxx dove serve)
- [ ] Numeri letti da `RulesConfig`

## Report
(append-only)
