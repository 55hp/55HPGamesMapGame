// ===== Assets/MapGame/Features/Gameplay/HexGrid/ClusterPatternDefinition.cs =====
using System;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Contenuto di una singola tile all'interno di un cluster estetico.
    /// </summary>
    [Serializable]
    public struct ClusterTileSpec
    {
        public TileType Type;
        public int HpRestore;
        public int MoneteGained;
    }

    /// <summary>
    /// Pattern fisso di un cluster estetico (GDD: "Aesthetic Clusters"), superato dal
    /// sistema EventClusterShape/EventClusterCatalog gia' implementato — questo file non
    /// e' piu' referenziato dalla pipeline live. Aggiornato 2026-07-10 solo per restare
    /// compilabile dopo il refactor TileType (Trappola -> Mistery, Battaglia -> Enemy).
    /// -- Franci TASK -- se definitivamente superato, valuta di rimuoverlo.
    ///
    /// Forma a "fiore" esagonale: un centro + le sei tile adiacenti (petali),
    /// indicizzate secondo l'ordine di HexCoord.GetNeighbor(dir), dir 0..5.
    ///
    /// Il pattern NON contiene posizione: veniva applicato a un centro scelto
    /// a runtime dal generatore di allora.
    /// </summary>
    [Serializable]
    public sealed class ClusterPatternDefinition
    {
        public string Name;
        public ClusterTileSpec Center;
        public ClusterTileSpec[] Petals; // lunghezza fissa 6, indice = direzione

        public ClusterPatternDefinition(string name, ClusterTileSpec center, ClusterTileSpec[] petals)
        {
            if (petals == null || petals.Length != 6)
                throw new ArgumentException("Un pattern di cluster deve avere esattamente 6 petali.", nameof(petals));

            Name = name;
            Center = center;
            Petals = petals;
        }
    }

    /// <summary>
    /// Pattern hardcoded storico, non piu' usato dalla pipeline live.
    /// </summary>
    public static class ClusterPatternCatalog
    {
        /// <summary>
        /// Cluster "Foresta": centro = mostro forte, petali = mix Mistery/Risorsa.
        /// Esempio storico dal GDD, Trappola sostituito con Mistery (2026-07-10, Trappola
        /// non e' piu' un TileType a se').
        /// </summary>
        public static ClusterPatternDefinition Foresta => new ClusterPatternDefinition(
            "Foresta",
            center: new ClusterTileSpec { Type = TileType.Enemy, HpRestore = -8, MoneteGained = 4 },
            petals: new[]
            {
                new ClusterTileSpec { Type = TileType.Chance, HpRestore =  0, MoneteGained = 0 },
                new ClusterTileSpec { Type = TileType.Goods, HpRestore =  4, MoneteGained = 0 },
                new ClusterTileSpec { Type = TileType.Chance, HpRestore =  0, MoneteGained = 0 },
                new ClusterTileSpec { Type = TileType.Chance, HpRestore =  0, MoneteGained = 0 },
                new ClusterTileSpec { Type = TileType.Goods, HpRestore =  4, MoneteGained = 0 },
                new ClusterTileSpec { Type = TileType.Chance, HpRestore =  0, MoneteGained = 0 },
            });
    }
}
