# 0004 — Generatore a censimento

**Stato**: da fare · **Commit di riferimento**: —

## Obiettivo
Generatore puro sparso con seed (04_mappa).

## Contratto (Athena)
R-001, R-007..R-009, 04_mappa. `CensusMapGenerator` (Rules) → `MapLayout` con tile, `ClueCounts` per riga/colonna. Adapter in Features che implementa `IMapGenerator` esistente.

## Lato codice (Claude Code)
Start/Boss: porta in Rules `PlaceStart`/`PlaceEnd` di `MapClusterGenerator` senza cambiarne il comportamento (R-008, D-004/005); riusa `MapGenerationConfig`, `IMapGenerator`, `MapGenerationResult`, pipeline del seed (CurrentRunSeed). Fisher–Yates su `IRandomSource`. Validazione con `Success/FailureReason`. 1000 seed nei test: somma 100, conteggi per tipo, Boss≠Start, determinismo, ClueCounts corretti.

## Lato Editor (Bezi) — checklist
- Nessuna (nessun asset nuovo).

## Per Franci
- Lancia in Unity i test EditMode indicati nel report.
- Commit/push quando i test sono verdi.

## Verifica
- [ ] `dotnet test Tools/RulesHarness` verde (se tocca Rules)
- [ ] Nessuna regola nuova inventata (TODO R-xxx dove serve)
- [ ] Numeri letti da `RulesConfig`

## Report
(append-only)
