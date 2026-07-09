using System;
using UnityEngine;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    [Serializable]
    public struct TileTypeSprite
    {
        public TileType Type;
        public Sprite Icon;
    }

    /// <summary>
    /// Punto unico per gli sprite delle tile: uno o più prefab HexTileView puntano allo
    /// stesso asset invece di avere ciascuno la propria copia dei riferimenti. Aggiungere
    /// un TileType richiede solo una nuova riga in TypeIcons qui, non un nuovo campo nel
    /// prefab né un nuovo case nello switch di HexTileView.
    /// </summary>
    [CreateAssetMenu(menuName = "MapGame/Tile Visual Config", fileName = "TileVisualConfig")]
    public class TileVisualConfig : ScriptableObject
    {
        [Header("Sprite stato non-risolto")]
        public Sprite SconosciutaSprite;
        public Sprite ConosciutaSprite;

        [Header("Icone evento per tipo (mostrate al Reveal)")]
        public TileTypeSprite[] TypeIcons;

        /// <summary>Null se il tipo non ha ancora un'icona assegnata in TypeIcons.</summary>
        public Sprite GetIcon(TileType type)
        {
            if (TypeIcons == null) return null;

            foreach (var entry in TypeIcons)
            {
                if (entry.Type == type) return entry.Icon;
            }

            return null;
        }
    }
}
