# 0012 — Pulizia legacy e bot di bilanciamento

**Stato**: fatto (bot e pulizia) · **Commit di riferimento**: —

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
- [ ] `Tools/RulesHarness/run.sh` verde (se tocca Rules)
- [ ] Nessuna regola nuova inventata (TODO R-xxx dove serve)
- [ ] Numeri letti da `RulesConfig`

## Report
(append-only)

### Athena — 2026-11-03 — branch `athena/v2-impl`
- `RandomPolicy`, `GreedyPolicy`, `BalanceSimulator` in Rules/Bot. Esecuzione: `Tools/RulesHarness/run.sh --sim 10000 [file.csv] [Nome=valore …]`.
- Risultati 10 000 partite (D-031 applicata): Greedy 48,2% vittorie, Random 0%. Con Attacco base 0 (regole originali) Greedy 2,1%. HP 6 → 62%, HP 7 → 78% (non applicato: HP 5 è una decisione chiusa).
- Pulizia legacy (MapClusterGenerator, Food, analyzer a cluster, popup v1) non ancora fatta.

### Athena — 2026-11-03 (seconda parte) — pulizia legacy
- Rimossi (codice): `MapClusterGenerator`, gli analyzer a cluster/strade/reachability, `UIPopup_Encounter`, `UIPopup_Shop`, `RevealEffectPopupDispatcher`, `MapGameGameplayHud`, `SurvivalConfig`, `TraderConfig`, `EnemyConfig`, `EncounterEvents`, `TradeEvents`, gli stub v1 di `HexGridController`.
- Scollegati e rimossi da Bezi prima del codice: dispatcher dalla scena, voci Addressables e prefab dei due popup, `UIGameHUD`, i tre config dal `MainConfigCatalog`.
- Lasciati di proposito (inerti, non toccano la partita): `LevelConfig`, `EventClusterShape`/`Catalog` e i loro editor, i campi cluster di `MapGenerationConfig`, `Food`/`HasKeyDL6` in `IGameContextService` (template Core, non modificabile).
