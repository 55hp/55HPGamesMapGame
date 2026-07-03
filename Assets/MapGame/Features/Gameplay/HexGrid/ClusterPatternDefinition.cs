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
    /// Pattern fisso di un cluster estetico (GDD: "Aesthetic Clusters").
    /// Forma a "fiore" esagonale: un centro + le sei tile adiacenti (petali),
    /// indicizzate secondo l'ordine di HexCoord.GetNeighbor(dir), dir 0..5.
    ///
    /// Il pattern NON contiene posizione: viene applicato a un centro scelto
    /// a runtime da AestheticClusterMapGenerator.
    ///
    /// Dato puro, nessuna logica. Il wrapping in ScriptableObject (editabile
    /// da Inspector) è demandato a Bezi nel prossimo step.
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
    /// Pattern hardcoded per il test di Sessione 1. Diventerà un catalogo di
    /// asset ScriptableObject quando pronto il wrapping (prossimo step, Bezi).
    /// </summary>
    public static class ClusterPatternCatalog
    {
        /// <summary>
        /// Cluster "Foresta": centro = mostro forte, petali = mix Trappola/Risorsa/Mistery.
        /// Esempio di riferimento dal GDD per il test di leggibilità.
        /// </summary>
        public static ClusterPatternDefinition Foresta => new ClusterPatternDefinition(
            "Foresta",
            center: new ClusterTileSpec { Type = TileType.Battaglia, HpRestore = -8, MoneteGained = 4 },
            petals: new[]
            {
                new ClusterTileSpec { Type = TileType.Trappola, HpRestore = -3, MoneteGained = 0 },
                new ClusterTileSpec { Type = TileType.Risorsa,  HpRestore =  4, MoneteGained = 0 },
                new ClusterTileSpec { Type = TileType.Trappola, HpRestore = -3, MoneteGained = 0 },
                new ClusterTileSpec { Type = TileType.Mistery,  HpRestore =  0, MoneteGained = 0 },
                new ClusterTileSpec { Type = TileType.Risorsa,  HpRestore =  4, MoneteGained = 0 },
                new ClusterTileSpec { Type = TileType.Trappola, HpRestore = -3, MoneteGained = 0 },
            });
    }
}