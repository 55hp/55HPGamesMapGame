using UnityEngine;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Config visiva di un singolo TileType: un asset per tipo, stesso pattern di
    /// EventClusterShape (un asset per forma) + EventClusterCatalog (raccolta). Qui il
    /// raccoglitore e' HexTileConfigCatalog. Sostituisce le entry TypeIcons di
    /// TileVisualConfig, revisione 2026-07-10.
    ///
    /// Permette anche di dare a una entry ListType.StopSingle (es. un NPC riskinnato)
    /// uno sprite tematico diverso da quello "standard" dello stesso TileType: basta
    /// creare un secondo HexTileConfig per lo stesso TileType e scegliere quale asset
    /// mettere nel catalogo/nella entry pertinente — la ricerca in
    /// HexTileConfigCatalog.GetIcon usa il primo match per TileType nell'array, quindi se
    /// serve piu' di uno sprite per lo stesso tipo la selezione va gestita a monte
    /// (fuori scope di questa consegna, -- Franci TASK -- se/quando serve davvero).
    /// </summary>
    [CreateAssetMenu(menuName = "MapGame/Hex Tile Config", fileName = "HexTileConfig")]
    public sealed class HexTileConfig : ScriptableObject
    {
        public TileType Type;
        public Sprite Icon;

        [Tooltip("Etichetta TMP mostrata insieme a Icon sulla tile (componente 2b dell'anatomia visiva, 2026-08-06). Vuota = nessun testo mostrato per questo tipo.")]
        public string Label;
    }
}
