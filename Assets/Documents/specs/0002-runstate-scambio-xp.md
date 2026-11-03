# 0002 — RunState, equipaggiamento, scambio, XP

**Stato**: fatto (codice e test) · **Commit di riferimento**: —

## Obiettivo
Implementare lo stato di run e la risoluzione di incontro, XP e progressione, tutto in Rules.

## Contratto (Athena)
R-020..R-025, R-029, R-031..R-036, R-040..R-044, R-038 (bonus monete a ogni guadagno), R-043. `RunState`, `EquipmentSlots`, `AttackCalculator`, `EncounterResolver.Resolve(runState, defense)` → `EncounterResult` (Won/Fled, danno, ricompensa). `XpService` con linee completate e livelli.

## Lato codice (Claude Code)
Tutto in `Assets/MapGame/Rules/`. Eventi di regola come tipi puri (`DamageTaken`, `XpGained`, `LevelUpAvailable`, …). I numeri da `RulesConfig` (stub finché 0003 non c'è).

## Lato Editor (Bezi) — checklist
- Nessuna.

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
- `RunState` (Rules/Run), `RulesEngine` con scambio unico (R-031..R-034), danno `max(0, Difesa − Attacco)`, XP e level up (R-040..R-043), bonus monete per evento (R-038).
- Al posto di `AttackCalculator`/`EncounterResolver`/`XpService` separati c'è un solo `RulesEngine` a decisioni pendenti (01 §6): le regole restano in un punto solo. Attacco calcolato da `RunState.Attack`.
- Eventi: un tipo piatto `RuleEvent` con `RuleEventType` (più semplice da tradurre in Features).
- Test: `RulesEngineTests` (R-020..R-043).
