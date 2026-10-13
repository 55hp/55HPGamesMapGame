# 0010 — Forzieri, chiavi, ricompense mini-boss

**Stato**: da fare · **Commit di riferimento**: —

## Obiettivo
Forzieri semplici/argento/oro, chiavi come flag, ricompense dei 4 tipi di mini-boss.

## Contratto (Athena)
R-037, R-053, R-054, 03 §4–§5. Forziere senza chiave: svanisce, tile consumata, contenuto perso (R-054). Nessun forziere riapribile.

## Lato codice (Claude Code)
Test per ogni contenuto fisso.

## Lato Editor (Bezi) — checklist
- Icone chiave nell'HUD; popup forziere.

## Per Franci
- Lancia in Unity i test EditMode indicati nel report.
- Commit/push quando i test sono verdi.

## Verifica
- [ ] `dotnet test Tools/RulesHarness` verde (se tocca Rules)
- [ ] Nessuna regola nuova inventata (TODO R-xxx dove serve)
- [ ] Numeri letti da `RulesConfig`

## Report
(append-only)
