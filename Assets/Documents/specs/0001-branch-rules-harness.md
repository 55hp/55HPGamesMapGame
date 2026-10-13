# 0001 — Branch v2, assembly Rules, harness

**Stato**: da fare · **Commit di riferimento**: —

## Obiettivo
Creare le fondamenta: branch `v2`, assembly puro `hp55games.MapGame.Rules`, progetto `Tools/RulesHarness`.

## Contratto (Athena)
Cartella `Assets/Game/Rules/` con asmdef (`noEngineReferences: true`, namespace `hp55games.MapGame.Rules`). `IRandomSource` + implementazione xoshiro/SplitMix64 seedata. `Tools/RulesHarness/RulesHarness.csproj` (net8.0, LangVersion 9.0, NUnit) che linka `Assets/Game/Rules/**/*.cs` e `Assets/Tests/EditMode/Rules/**/*.cs`. `Game.Features.asmdef` riferisce Rules (GUID).

## Lato codice (Claude Code)
Rules: `IRandomSource`, `SplitMix64Random`. Test: stessa seed = stessa sequenza; seed diversi = sequenze diverse. `dotnet test Tools/RulesHarness` verde.

## Lato Editor (Bezi) — checklist
- Nessuna: l'asmdef di Rules viene riconosciuta da Unity al ritorno sull'Editor (Franci).

## Per Franci
- Lancia in Unity i test EditMode indicati nel report.
- Commit/push quando i test sono verdi.

## Verifica
- [ ] `dotnet test Tools/RulesHarness` verde (se tocca Rules)
- [ ] Nessuna regola nuova inventata (TODO R-xxx dove serve)
- [ ] Numeri letti da `RulesConfig`

## Report
(append-only)
