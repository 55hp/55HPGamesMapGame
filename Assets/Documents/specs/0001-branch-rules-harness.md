# 0001 — Branch v2, assembly Rules, harness

**Stato**: fatto (codice) · verifica Unity in corso · **Commit di riferimento**: —

## Obiettivo
Creare le fondamenta: branch `v2`, assembly puro `hp55games.MapGame.Rules`, progetto `Tools/RulesHarness`.

## Contratto (Athena)
Cartella `Assets/MapGame/Rules/` con asmdef (`noEngineReferences: true`, namespace `hp55games.MapGame.Rules`). `IRandomSource` + implementazione xoshiro/SplitMix64 seedata. `Tools/RulesHarness/RulesHarness.csproj` (net472, LangVersion 9.0, NUnit di Unity) che linka `Assets/MapGame/Rules/**/*.cs` e `Assets/Tests/EditMode/Rules/**/*.cs`. `Game.Features.asmdef` riferisce Rules (per nome: il `.meta` dell'asmdef lo genera Unity).

## Lato codice (Claude Code)
Rules: `IRandomSource`, `SplitMix64Random`. Test: stessa seed = stessa sequenza; seed diversi = sequenze diverse. `Tools/RulesHarness/run.sh` verde.

## Lato Editor (Bezi) — checklist
- Nessuna: l'asmdef di Rules viene riconosciuta da Unity al ritorno sull'Editor (Franci).

## Per Franci
- Lancia in Unity i test EditMode indicati nel report.
- Commit/push quando i test sono verdi.

## Verifica
- [x] `Tools/RulesHarness/run.sh` verde (se tocca Rules)
- [x] Nessuna regola nuova inventata (TODO R-xxx dove serve)
- [ ] Numeri letti da `RulesConfig`

## Report
(append-only)

### Athena — 2026-11-03 — branch `athena/v2-impl`
Sessione di implementazione delegata ad Athena (Franci: "delega il più possibile"). Athena fa anche il "Lato codice" in questa sessione.

- Rules in `Assets/MapGame/Rules/` invece di `Assets/Game/Rules/`: coerente con `Assets/MapGame/Features/`. Documenti aggiornati.
- `IRandomSource` (NextULong, Range con rifiuto anti-bias, NextDouble) + `SplitMix64Random`.
- Test in `Assets/Tests/EditMode/Rules/` con asmdef propria `hp55games.MapGame.Rules.Tests` (riferimenti per nome).
- Harness: NuGet non raggiungibile dall'ambiente degli agenti, quindi niente `dotnet test`. Il progetto (net472) usa la NUnit di Unity da `Library/PackageCache` e un runner minimale (`Runner.cs`: Test, TestCase, SetUp, TearDown). Su Linux gira con Mono (`run.sh`), su Windows con `dotnet run --project Tools/RulesHarness`. Esito: 5/5 verdi.

#### Per Bezi
- Nessuna.

#### Per Franci
- Nessuna azione: la verifica in Unity (compilazione, Test Runner EditMode) la fa Athena.

