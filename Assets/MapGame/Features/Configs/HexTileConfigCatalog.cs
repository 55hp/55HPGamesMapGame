using UnityEngine;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Punto unico per gli sprite delle tile. Sostituisce TileVisualConfig, revisione
    /// 2026-07-10: le icone per tipo sono ora asset HexTileConfig individuali (stesso
    /// pattern di EventClusterShape/EventClusterCatalog) invece di un array inline, cosi'
    /// aggiungere/modificare l'icona di un tipo non richiede toccare questo asset condiviso.
    /// I due sprite di stato (Sconosciuta/Conosciuta) restano qui perche' sono globali,
    /// non per-tipo.
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
    }
}
