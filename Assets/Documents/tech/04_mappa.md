# 04 — Mappa e generazione

## 1. Griglia

- 10×10 esagoni, assiale (Q,R) in memoria; il disegno usa odd-q (come `HexCoord.FromOffsetOddQ` del codice esistente).
- Etichette: colonne A–J (da sinistra), righe 1–10 (dall'alto); la tile "B3" è colonna B, riga 3.
- `width`, `height` e `seed` vengono da `MapGenerationConfig` (esistente, default 10×10). Seed: come oggi, `IGameContextService.CurrentRunSeed` se ≠ 0, altrimenti `MapGenerationConfig.Seed` (D-024: si usa l'implementazione esistente). Dimensione ≠ 10×10 **non** è supportata nel livello 1: il censimento somma a 100.

## 2. Generazione: censimento sparso con seed

Algoritmo (R-007, R-009). Deve essere puro (Rules, nessun `UnityEngine`) e deterministico.

1. Costruire il multiinsieme di `ContentKind` dal censimento (`03_contenuti.md` §1) letto da `RulesConfig`. La somma deve essere `width × height`; altrimenti errore esplicito (non correggere in silenzio).
2. Mescolare con un Fisher–Yates su `IRandomSource` (PRNG proprio, p. es. SplitMix64/xoshiro; in Rules **vietato `System.Random` e `UnityEngine.Random`**; l'adapter in Features passa il seed), seedato.
3. **Start e Boss: logica già implementata, da riusare senza cambiarla** (D-004/D-005, Franci): portare in Rules (con una coordinata assiale pura al posto di `HexCoord`) `PlaceStart` e `PlaceEnd` di `MapClusterGenerator`: Start a caso tra le tile ad almeno `StartMinBorderDistance` dal bordo; Boss alla tile con distanza più vicina a quella estratta da `EndDistanceWeights`. Valori letti da `MapGenerationConfig` (esistente). La partenza è Explored; il Boss resta coperto e non Spotted (R-008, D-025).
   - Nessun altro vincolo di posizionamento: niente cluster, niente adiacenze imposte.
4. Assegnare le tile in ordine di riga; la partenza e le Vuote nascono scoperte.
5. Calcolare per ogni riga e colonna il conteggio di ogni `ClueTarget` (`05_indizi.md`), una volta sola, e metterlo nel risultato (`MapLayout.ClueCounts`).
6. Validare: totale = W×H; Boss unico; i 5 mini-boss presenti nelle 4 varianti previste; ciascuna chiave ha il suo forziere. In caso di errore il risultato ha `Success = false` e `FailureReason` (come l'`IMapGenerator` esistente).

La mappa non garantisce una partita vincibile (una sola mossa fortunata o sfortunata può decidere): è voluto, la vittoria dipende da quanto il giocatore deduce e accumula prima del Boss. La simulazione (`07_test-e-simulazione.md`) misura quanto è dura.

## 3. Livello di mascheramento sulla tile

`DifficultyLevel` (1–6) è una proprietà **del tipo di contenuto**, non dei vicini: la tabella è R-081 e vive in `RulesConfig`. Il vecchio calcolo `min(richiesto, vicini)` di `MapClusterGenerator.ResolveDifficulty` **non si usa più**.

## 4. Integrazione con il codice esistente

- `IMapGenerator.Generate(width, height, seed)` e `MapGenerationResult` restano l'interfaccia verso `HexGridController`. Nuova implementazione `CensusMapGenerator` in Features, che chiama il generatore puro in Rules e traduce in `HexTileData`.
- `MapGenerationService.GenerateMap` smette di istanziare `MapClusterGenerator`.
- Principio (D-019, Franci): il sistema di creazione della mappa riusa ciò che esiste già. `MapGenerationConfig`: restano usati `Width`, `Height`, `Seed`, `EndDistanceWeights`, `StartMinBorderDistance`; diventano inutilizzati `ClusterMinDistanceFromStartEnd`, `StradaNetwork`, `EventClusters` (non cancellare gli asset finché Bezi non ha ripulito: spec 0012). Si riusano anche `IMapGenerator`, `MapGenerationResult`, `HexTileData`, la pipeline del seed e `HexGridController.BuildGrid`.
- Gli analyzer di `HexDebugAdapter` basati su cluster (budget, spacing, reachability) vanno disattivati o riscritti (spec 0012). `ReachabilityAnalyzer` non ha più senso con il click libero.
