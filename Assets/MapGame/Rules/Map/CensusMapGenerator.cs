using System;
using System.Collections.Generic;

namespace hp55games.MapGame.Rules
{
    /// <summary>
    /// Generatore a censimento sparso (04_mappa §2, R-007..R-009). Puro e deterministico.
    /// Start e Boss: stessa logica di MapClusterGenerator.PlaceStart/PlaceEnd (D-004/D-005),
    /// con la casualità presa da IRandomSource invece che da System.Random.
    /// </summary>
    public static class CensusMapGenerator
    {
        public static MapLayout Generate(RulesConfig config, ulong seed)
        {
            var layout = new MapLayout { Width = config.Width, Height = config.Height, Seed = seed };
            var errors = config.Validate();
            if (errors.Count > 0)
            {
                layout.Success = false;
                layout.FailureReason = "RulesConfig non valida: " + string.Join("; ", errors);
                return layout;
            }

            int w = config.Width, h = config.Height, n = w * h;
            var rng = new SplitMix64Random(seed);
            var tiles = new ContentKind[n];
            var assigned = new bool[n];

            int start = PlaceStart(config, rng);
            int boss = PlaceEnd(config, start, rng);
            tiles[start] = ContentKind.Start; assigned[start] = true;
            tiles[boss] = ContentKind.Boss; assigned[boss] = true;

            // Multiinsieme del resto del censimento, in ordine di enum (deterministico), poi Fisher–Yates.
            var bag = new List<ContentKind>(n);
            foreach (ContentKind kind in Enum.GetValues(typeof(ContentKind)))
            {
                if (kind == ContentKind.Start || kind == ContentKind.Boss) continue;
                if (!config.Census.TryGetValue(kind, out int qty)) continue;
                for (int i = 0; i < qty; i++) bag.Add(kind);
            }
            for (int i = bag.Count - 1; i > 0; i--)
            {
                int j = rng.Range(0, i + 1);
                var tmp = bag[i]; bag[i] = bag[j]; bag[j] = tmp;
            }

            int k = 0;
            for (int i = 0; i < n; i++)
            {
                if (assigned[i]) continue;
                tiles[i] = bag[k++];
            }

            layout.Tiles = tiles;
            layout.StartIndex = start;
            layout.BossIndex = boss;

            // Conteggi per gli indizi (05 §4), una volta sola.
            foreach (ClueTarget target in Enum.GetValues(typeof(ClueTarget)))
            {
                for (int r = 0; r < h; r++)
                {
                    int count = 0;
                    for (int c = 0; c < w; c++) if (Matches(config, tiles[r * w + c], target)) count++;
                    layout.SetClueCount(target, LineRef.Row(r), count);
                }
                for (int c = 0; c < w; c++)
                {
                    int count = 0;
                    for (int r = 0; r < h; r++) if (Matches(config, tiles[r * w + c], target)) count++;
                    layout.SetClueCount(target, LineRef.Col(c), count);
                }
            }

            layout.Success = true;
            return layout;
        }

        public static bool Matches(RulesConfig config, ContentKind kind, ClueTarget target) =>
            config.Contents.TryGetValue(kind, out var def) && def.ClueTarget == target;

        private static int PlaceStart(RulesConfig config, IRandomSource rng)
        {
            int w = config.Width, h = config.Height, b = config.StartMinBorderDistance;
            var candidates = new List<int>();
            for (int row = 0; row < h; row++)
            for (int col = 0; col < w; col++)
            {
                if (col < b || col >= w - b) continue;
                if (row < b || row >= h - b) continue;
                candidates.Add(HexGridMath.Index(col, row, w));
            }
            return candidates.Count == 0
                ? HexGridMath.Index(w / 2, h / 2, w)
                : candidates[rng.Range(0, candidates.Count)];
        }

        private static int PlaceEnd(RulesConfig config, int start, IRandomSource rng)
        {
            int target = WeightedPick(config.EndDistanceWeights, rng);
            int bestDelta = int.MaxValue;
            var best = new List<int>();
            for (int i = 0; i < config.Width * config.Height; i++)
            {
                if (i == start) continue;
                int delta = Math.Abs(HexGridMath.Distance(i, start, config.Width) - target);
                if (delta < bestDelta) { bestDelta = delta; best.Clear(); }
                if (delta == bestDelta) best.Add(i);
            }
            // D-029 (Athena, da rivedere): a parità di distanza si sceglie a caso, non la prima
            // in ordine di scansione come nel codice v1 (che metteva il Boss quasi sempre in alto
            // rispetto alla partenza, rendendolo prevedibile).
            return best[rng.Range(0, best.Count)];
        }

        private static int WeightedPick(List<DistanceWeight> table, IRandomSource rng)
        {
            int total = 0;
            foreach (var e in table) total += e.Weight;
            int roll = rng.Range(0, total);
            foreach (var e in table)
            {
                if (roll < e.Weight) return e.Distance;
                roll -= e.Weight;
            }
            return table[table.Count - 1].Distance;
        }
    }
}
