using System.Collections.Generic;
using hp55games.Mobile.Core.Config;
using UnityEngine;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Raccolta di tutti gli ElementConfig per-specie del gioco, con lookup per
    /// TileType + DifficultyLevel. Stesso pattern di EventClusterCatalog /
    /// HexTileConfigCatalog: gli asset si trascinano a mano oppure si caricano con il
    /// bottone del suo Editor custom (vedi ElementCatalogEditor, scansiona
    /// Assets/MapGame/Content/Elements/).
    ///
    /// Risoluzione (GDD, Tile &amp; Element Knowledge System): la run ha un numero fisso
    /// di Element per tipo dal LevelConfig; il generatore, per ogni tile content con
    /// {TileType, DifficultyLevel} risolto, pesca una specie eleggibile da qui
    /// (AestheticClusterMapGenerator.ApplyElementStats chiama PickRandom).
    /// </summary>
    [CreateAssetMenu(menuName = "MapGame/Element Catalog", fileName = "ElementCatalog")]
    public sealed class ElementCatalog : ScriptableObject, IConfigAsset
    {
        [Tooltip("Tutti gli ElementConfig del gioco, uno per specie. Popolare a mano o col bottone dell'Editor custom.")]
        public ElementConfig[] Elements;

        /// <summary>
        /// Tutte le specie eleggibili per il TileType e DifficultyLevel dati. Lista vuota
        /// (mai null) se nessuna specie combacia — il chiamante decide il fallback.
        /// </summary>
        public IReadOnlyList<ElementConfig> GetEligible(TileType type, int difficultyLevel)
        {
            var result = new List<ElementConfig>();
            if (Elements == null) return result;

            foreach (var element in Elements)
            {
                if (element != null && element.Type == type && element.DifficultyLevel == difficultyLevel)
                    result.Add(element);
            }

            return result;
        }

        /// <summary>
        /// Una specie eleggibile scelta a caso col System.Random del chiamante (il seed di
        /// run resta l'unica fonte di casualita', mai UnityEngine.Random qui). Null se
        /// nessuna specie combacia.
        /// </summary>
        public ElementConfig PickRandom(TileType type, int difficultyLevel, System.Random rng)
        {
            var eligible = GetEligible(type, difficultyLevel);
            if (eligible.Count == 0) return null;
            return eligible[rng.Next(eligible.Count)];
        }

        /// <summary>Prima specie con lo SpeciesId dato, null se assente. Per lookup diretti (salvataggi, debug).</summary>
        public ElementConfig FindBySpeciesId(string speciesId)
        {
            if (Elements == null || string.IsNullOrEmpty(speciesId)) return null;

            foreach (var element in Elements)
            {
                if (element != null && element.SpeciesId == speciesId)
                    return element;
            }

            return null;
        }
    }
}
