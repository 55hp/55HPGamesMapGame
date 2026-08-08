using UnityEngine;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Config visiva (e ora anche comportamentale) di un singolo TileType: un asset per
    /// tipo, stesso pattern di EventClusterShape (un asset per forma) + EventClusterCatalog
    /// (raccolta). Qui il raccoglitore e' HexTileConfigCatalog.
    ///
    /// Reveal: cosa succede al reveal di una tile di questo tipo (vedi RevealEffect).
    /// Relazione 1:1 stabile e permanente con il TileType, quindi vive direttamente qui
    /// invece che su un asset Element separato — un solo campo non giustificava una
    /// classe/asset a parte con un solo riferimento da tenere sincronizzato.
    ///
    /// Permette anche di dare a una entry StopTileEntries (es. un NPC riskinnato)
    /// uno sprite tematico diverso da quello "standard" dello stesso TileType: basta
    /// creare un secondo HexTileConfig per lo stesso TileType e scegliere quale asset
    /// mettere nel catalogo/nella entry pertinente — la ricerca in
    /// HexTileConfigCatalog.GetIcon usa il primo match per TileType nell'array, quindi se
    /// serve piu' di uno sprite per lo stesso tipo la selezione va gestita a monte.
    /// </summary>
    [CreateAssetMenu(menuName = "MapGame/Hex Tile Config", fileName = "HexTileConfig")]
    public sealed class HexTileConfig : ScriptableObject
    {
        public TileType Type;
        public Sprite Icon;

        [Tooltip("Etichetta TMP mostrata insieme a Icon sulla tile (componente 2b dell'anatomia visiva). Vuota = nessun testo mostrato per questo tipo.")]
        public string Label;

        [Tooltip("Cosa succede al reveal di una tile di questo tipo.")]
        public RevealEffect Reveal;
    }
}
