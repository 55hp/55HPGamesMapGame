# 01 — Architettura (v2)

Base: branch `v2` da `develop` @ `fdfe080fbf927808e80481022913526f5b35384e`. Il codice esistente (template Core + Features) si riusa; si aggiunge un livello di regole puro. Verificato su GitHub reale, non su report di Bezi.

## 1. Assembly

| Assembly | Namespace | Contenuto | Riferimenti engine |
|---|---|---|---|
| `Core.Runtime`, `UI.Runtime` (template) | `hp55games.Mobile.Core.*` | ServiceRegistry, EventBus, FSM, popup, Addressables | sì (esistenti, non toccare) |
| **`hp55games.MapGame.Rules`** (nuovo) | `hp55games.MapGame.Rules` | tutta la logica di regole: RunState, equipaggiamento, scambio, XP, NPC, indizi, generatore, `RulesConfig`, `IRandomSource` | **no** (`noEngineReferences: true`) |
| `Game.Features` (esistente) | `hp55games.MapGame.Features.*` | `HexGridController`, UI, adapter verso Rules, ScriptableObject | sì |
| `Game.Tests.EditMode` (esistente) | — | test Unity; i test delle regole girano anche fuori da Unity (§3) | — |

**[DEFAULT D-003]** Rules è un assembly puro. `Game.Features` lo riferisce (aggiungere il GUID all'asmdef: lavoro Editor-neutral, solo testo JSON, lo fa Claude Code).

## 2. Regole d'oro (per Claude Code)

1. Rules: niente `UnityEngine`, `System.Random`, `DateTime.Now`, I/O. Casualità solo da `IRandomSource`; stesso seed + stesse scelte = stessi eventi.
2. C# 9 al massimo, API .NET Standard 2.1.
3. La presentazione non modifica lo stato di regole: legge `RunState` (sola lettura) e gli eventi; agisce solo chiamando i servizi di Rules.
4. Ogni decisione espone le sue opzioni legali (UI, tutorial, bot di bilanciamento).
5. Numeri di regola solo in `RulesConfig`.
6. Nessun singleton o statico mutabile; si usa `ServiceRegistry` del template.
7. Testi per il giocatore in italiano, in `UiText` (Features) oppure nella localizzazione del template (`localization_master.txt`), mai hardcoded nei prefab.
8. Niente `AddComponent`/`Find*` a runtime per oggetti autorabili; `[SerializeField]` + checklist per Bezi.

## 3. Test fuori da Unity

`Tools/RulesHarness/` (progetto .NET, `net8.0`, `LangVersion 9.0`, NUnit) include per link i sorgenti `Assets/Game/Rules/**/*.cs` e i test `Assets/Tests/EditMode/Rules/**/*.cs`. Si esegue con `dotnet test Tools/RulesHarness`. Non si genera da Unity e non entra nei `.csproj` di Unity. Spec 0001.

## 4. Stato di run

**[DEFAULT D-006]** `RunState` v2 (HP, HP max, Attacco, XP, livello, monete, slot Arma/Anello/Trinket, bonus Fabbro, flag chiavi, indizi posseduti, linee usate) vive in Rules. Il `IGameContextService` esistente (ProfileId, Score, Lives, Food, HasKeyDL4/5/6, ResetRun) viene **sostituito** per i campi di gioco: `Food` e `HasKeyDL*` spariscono; un adapter in Features pubblica sul bus gli eventi di cambio (HpChanged, CoinsChanged, XpChanged, …). Il template Core non si tocca: i nuovi eventi vivono in Features.

## 5. Tipi di tile

**[DEFAULT D-022]** `TileType` (int enum, valori 0–21) non si rinumera mai. Si aggiungono valori nuovi in coda per i contenuti mancanti (es. `MiniBoss`, `Trap`, `Chest*`). La distinzione fine tra varianti (MiniBossWeapon…, ChestSimpleBerries…) sta in `ContentKind` (Rules), che `HexTileData` porta come campo in più. La mappa tra `ContentKind` e `TileType` è un solo punto (`ContentKindMap`).

## 6. Flusso di una partita

```
GameBootstrap → carica RulesConfig (asset) → CensusMapGenerator(seed) → RunState nuovo
loop: HexTileTapController → GridService.TryReveal(coord)        // Features
      → RulesEngine.Reveal(coord)   (Rules, puro)                 // risolve scambio/NPC/forziere/…
      ← lista di eventi di regola → adapter → EventBus → HUD/popup
NPC/level up: il motore ritorna una *Decision* (con opzioni legali); la UI risponde con Submit(optionId)
```

`RulesEngine` è una macchina a decisioni pendenti (stile `GameSession.Submit` di Mareignoto): se una rivelazione richiede una scelta (NPC, level up), lo stato resta in attesa finché arriva una risposta valida.

## 7. Cosa si rimuove (spec 0012)

`MapClusterGenerator` e dipendenze, `Food*` ovunque, `Flee`, upgrade shop Food/Heal (sostituiti dagli NPC), `AestheticClusterMapGenerator` (citato nel vecchio CLAUDE.md), `ReachabilityAnalyzer`, `RecomputeReachability`. Non si cancellano asset prima che Bezi li abbia scollegati.

## 8. Delta codice → v2

| Area | Oggi (fdfe080) | v2 |
|---|---|---|
| Click | `IsClickable` richiede un vicino Explored | libero (R-002) |
| Generazione | `MapClusterGenerator` | `CensusMapGenerator` (04) |
| Incontro | Combatti/Fuggi, danno = DL | scambio unico (R-031..R-034), niente Fuggi |
| Cibo | `FoodCostPerClick`, `NoFoodHpPenalty` | rimosso |
| Stato | Lives, Food, Score, HasKeyDL4/5/6 | `RunState` v2 |
| NPC | Trader/Inn/Farmer via RevealEffect + `UIPopup_Shop` | pop-up scelta per NPC (spec 0008) |
| Indizi | non esiste | 05 |
| Missione | `IsObjective` | Caccia (R-070..R-073) |
| Test | solo smoke Core | test delle regole |
