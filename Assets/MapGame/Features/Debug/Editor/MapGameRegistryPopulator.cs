using System.Collections.Generic;
using hp55games.FranzTools.HexDebugFramework;
using hp55games.MapGame.Features.Gameplay.HexGrid;
using UnityEngine;
using UDebug = UnityEngine.Debug;

namespace hp55games.MapGame.Features.Debug.Editor
{
    /// <summary>
    /// Popola HexGridRegistry con i dati reali di MapGame prelevati da HexGridController.
    /// Costruisce un HexTileDebugCell per ogni HexTileData, associando la world position
    /// dal corrispondente HexTileView in scena.
    /// </summary>
    public static class MapGameRegistryPopulator
    {
        public static void Populate(HexGridRegistry registry)
        {
            var controller = Object.FindObjectOfType<HexGridController>();
            if (controller == null)
            {
                UDebug.LogWarning("[HexDebug] Nessun HexGridController trovato in scena. Impossibile popolare il registry.");
                return;
            }

            // Mappa coord → world position dai HexTileView in scena
            var views = Object.FindObjectsOfType<HexTileView>();
            var worldPositions = new Dictionary<HexCoord, Vector3>(views.Length);
            foreach (var view in views)
                worldPositions[view.Coord] = view.transform.position;

            var cells = new List<IHexCell>(controller.Tiles.Count);
            var startCoord = controller.PlayerCoord;

            foreach (var kvp in controller.Tiles)
            {
                var coord = kvp.Key;
                var data = kvp.Value;

                if (!worldPositions.TryGetValue(coord, out var worldPos))
                {
                    worldPos = Vector3.zero;
                    UDebug.LogWarning($"[HexDebug] HexTileView mancante per coord {coord}. WorldPosition impostata a zero.");
                }

                bool isStart = coord.Equals(startCoord);
                cells.Add(new HexTileDebugCell(data, worldPos, isStart));
            }

            registry.Populate(cells);
        }
    }
}
