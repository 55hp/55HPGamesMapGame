# 0011 — Missione Caccia

**Stato**: da fare · **Commit di riferimento**: —

## Obiettivo
Vittoria/sconfitta, Boss fuggito, indizi di partenza.

## Contratto (Athena)
R-039, R-070..R-073, R-098. 6 missioni (data/missions.csv), scelta a caso dal seed (D-026), 2 indizi di partenza in catena. `MissionRules`; `GameOver`/`Victory`. Boss `IsObjective`.

## Lato codice (Claude Code)
Test: 6 missioni con bersaglio e 2 indizi corretti; Boss con Attacco ≥ 8 vince; < 8 fugge → sconfitta; HP < 1 → sconfitta.

## Lato Editor (Bezi) — checklist
- Popup di fine partita.

## Per Franci
- Lancia in Unity i test EditMode indicati nel report.
- Commit/push quando i test sono verdi.

## Verifica
- [ ] `dotnet test Tools/RulesHarness` verde (se tocca Rules)
- [ ] Nessuna regola nuova inventata (TODO R-xxx dove serve)
- [ ] Numeri letti da `RulesConfig`

## Report
(append-only)
