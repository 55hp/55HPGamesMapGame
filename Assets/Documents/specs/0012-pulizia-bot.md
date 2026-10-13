# 0012 — Pulizia legacy e bot di bilanciamento

**Stato**: da fare · **Commit di riferimento**: —

## Obiettivo
Rimuovere il legacy e produrre il bot di simulazione.

## Contratto (Athena)
Architettura §7, 07 §3. Rimuovere `MapClusterGenerator`, Food*, analyzer a cluster; `RandomPolicy` e `GreedyPolicy`, CSV di output.

## Lato codice (Claude Code)
Eseguibile via `dotnet test` o console in `Tools/`. Output in `Tools/RulesHarness/out/` (ignorato da git).

## Lato Editor (Bezi) — checklist
- Scollegare gli asset legacy (MapGenerationConfig cluster, LevelConfig_Test_1) dalle scene prima della cancellazione.

## Per Franci
- Lancia in Unity i test EditMode indicati nel report.
- Commit/push quando i test sono verdi.

## Verifica
- [ ] `dotnet test Tools/RulesHarness` verde (se tocca Rules)
- [ ] Nessuna regola nuova inventata (TODO R-xxx dove serve)
- [ ] Numeri letti da `RulesConfig`

## Report
(append-only)
