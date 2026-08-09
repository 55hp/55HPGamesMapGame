using UnityEngine;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Punto unico per gli sprite delle tile. Le icone per tipo sono asset HexTileConfig
    /// individuali (stesso pattern di EventClusterShape/EventClusterCatalog) invece di un
    /// array inline, cosi' aggiungere/modificare l'icona di un tipo non richiede toccare
    /// questo asset condiviso. I due sprite di stato (Sconosciuta/Conosciuta) restano qui
    /// perche' sono globali, non per-tipo.
    ///
    /// TileConfigs si popola trascinando gli asset HexTileConfig a mano, oppure con il
    /// bottone "Carica tutte le config dalla cartella" nel suo Editor custom (vedi
    /// HexTileConfigCatalogEditor, scansiona Assets/MapGame/Content/TileConfigs/) —
    /// stesso pattern di EventClusterCatalogEditor.
    /// </summary>
    [CreateAssetMenu(menuName = "MapGame/Hex Tile Config Catalog", fileName = "HexTileConfigCatalog")]
    public sealed class HexTileConfigCatalog : ScriptableObject
    {
        [Header("Sprite stato non-risolto")]
        public Sprite SconosciutaSprite;
        public Sprite ConosciutaSprite;

        [Header("Config per tipo (un HexTileConfig per TileType)")]
        public HexTileConfig[] TileConfigs;

        [Header("Colore bordo per DifficultyLevel (componente 1 dell'anatomia visiva)")]
        [Tooltip("Indice = DifficultyLevel (0..6; 0 = Road/Void, strutturali, vedi HexTileData.DifficultyLevel). Voci non assegnate restano Color.clear finche' non impostate qui.")]
        public Color[] DifficultyLevelBorderColors = new Color[7];

        /// <summary>Null se il tipo non ha ancora una HexTileConfig assegnata in TileConfigs.</summary>
        public Sprite GetIcon(TileType type)
        {
            if (TileConfigs == null) return null;

            foreach (var entry in TileConfigs)
            {
                if (entry != null && entry.Type == type) return entry.Icon;
            }

            return null;
        }

        /// <summary>Null/vuoto se il tipo non ha ancora una HexTileConfig assegnata, o la sua Label e' vuota.</summary>
        public string GetLabel(TileType type)
        {
            if (TileConfigs == null) return null;

            foreach (var entry in TileConfigs)
            {
                if (entry != null && entry.Type == type) return entry.Label;
            }

            return null;
        }

        /// <summary>RevealEffect.Immediate (valore di default) se il tipo non ha ancora una HexTileConfig assegnata.</summary>
        public RevealEffect GetReveal(TileType type)
        {
            if (TileConfigs == null) return RevealEffect.Immediate;

            foreach (var entry in TileConfigs)
            {
                if (entry != null && entry.Type == type) return entry.Reveal;
            }

            return RevealEffect.Immediate;
        }

        /// <summary>Color.clear (non un colore di bordo valido) se difficultyLevel e' fuori dal range configurato — cosi' un bordo non assegnato e' visibilmente "mancante" invece di sembrare intenzionale.</summary>
        public Color GetDifficultyLevelColor(int difficultyLevel)
        {
            if (DifficultyLevelBorderColors == null || difficultyLevel < 0 || difficultyLevel >= DifficultyLevelBorderColors.Length)
                return Color.clear;

            return DifficultyLevelBorderColors[difficultyLevel];
        }
    }
}
