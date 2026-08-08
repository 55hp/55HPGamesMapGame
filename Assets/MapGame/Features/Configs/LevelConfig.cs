using System;
using System.Collections.Generic;
using hp55games.Mobile.Core.Config;
using UnityEngine;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Coppia TileType/DifficultyLevel comune alle tre liste di contenuto di LevelConfig
    /// (ClusterTileEntries/SingleTileEntries/StopTileEntries) — vedi le sottoclassi per il
    /// significato di Amount in ciascuna lista.
    /// </summary>
    [Serializable]
    public abstract class LevelTile
    {
        public TileType Type;

        [Range(1, 6)]
        public int DifficultyLevel;

        public abstract int Amount { get; }
    }

    /// <summary>
    /// Entry della palette EventCluster (LevelConfig.ClusterTileEntries): Type+DifficultyLevel
    /// eleggibili come centro (DL 4-6, vedi AestheticClusterMapGenerator.TryBuildProceduralCluster)
    /// o come contenuto del ring (DL 1-2). Amount forzato a 1 dal costruttore e ignorato dal
    /// generatore: ogni tipo di centro compare al massimo una volta per mappa a prescindere
    /// da Amount (vedi _usedClusterCenterTypes).
    /// </summary>
    [Serializable]
    public class ClusterTile : LevelTile
    {
        public override int Amount => 1;
    }

    /// <summary>
    /// Entry piazzabile come tessera singola isolata, o come ripiego del rammendo quando
    /// una tile non confina con nessun PathCluster (LevelConfig.SingleTileEntries). Amount
    /// libero: quante copie indipendenti di questa entry il generatore deve piazzare in una
    /// run (espanse PRIMA dello shuffle in AestheticClusterMapGenerator.BuildManifest).
    /// </summary>
    [Serializable]
    public class SingleTile : LevelTile
    {
        [Min(1)]
        [SerializeField]
        private int amount = 1;
        
        public override int Amount => amount;
    }

    /// <summary>
    /// Entry piazzabile come separatore tra due PathCluster distinti che si toccherebbero
    /// altrimenti (LevelConfig.StopTileEntries). Amount forzato a 1 dal costruttore e
    /// ignorato dal generatore: per piazzare N separatori servono N entry distinte, non
    /// un'unica entry con Amount=N.
    /// </summary>
    [Serializable]
    public class StopTyle : LevelTile
    {
        public override int Amount => 1;
    }

    [CreateAssetMenu(menuName = "MapGame/Level Config", fileName = "LevelConfig")]
    public sealed class LevelConfig : ScriptableObject, IConfigAsset
    {
        [Tooltip("Bioma del livello. Testo libero finche' non esiste un catalogo/enum Bioma formale (Three-Axis Visual Model, design-only ad oggi).")]
        public string EnvType;

        [Tooltip("Palette per lo Shape Editor/generazione procedurale EventCluster (centro ClusterMainTileEntries + ring ClusterFillerTileEntries) — vedi AestheticClusterMapGenerator.TryBuildProceduralCluster. Amount ignorato.")]
        public List<ClusterTile> ClusterMainTileEntries;
        public List<ClusterTile> ClusterFillerTileEntries;

        [Tooltip("Tessere singole isolate o di rammendo — vedi AestheticClusterMapGenerator.AssignSingleTile. Amount conta: ogni entry viene espansa in Amount copie indipendenti.")]
        public List<SingleTile> SingleTileEntries;

        [Tooltip("Separatori tra PathCluster distinti — vedi AestheticClusterMapGenerator.AssignStopTile. Amount ignorato: un'entry = un'istanza.")]
        public List<StopTyle> StopTileEntries;
    }
}
