using System.Collections.Generic;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Conta le sequenze di reveal valide (percorsi) da uno start a un obiettivo, dato un
    /// budget di food che si consuma al reveal (costo = distanza) e si ricarica sulle tile
    /// Risorsa (FoodReward). Usato per: 1) verificare che una mappa generata sia "beatable"
    /// (almeno 1 percorso), 2) classificarne la difficoltà (quanti percorsi esistono).
    ///
    /// Nota pragmatica: è uno strumento di test/classificazione, non codice di gameplay
    /// spedito al giocatore. Il conteggio è "capped" su due assi per restare utilizzabile
    /// su griglie 10x10-12x12: si ferma a maxPathsToFind percorsi trovati e non esplora
    /// oltre maxNodesToExplore stati, quindi il numero restituito è un lower bound "10+"
    /// oltre la soglia, non un conteggio esatto esaustivo.
    /// </summary>
    public sealed class MapPathCounter
    {
        private readonly Dictionary<HexCoord, HexTileData> _tiles;
        private readonly int _maxPathsToFind;
        private readonly int _maxNodesToExplore;
        private int _nodesExplored;

        public MapPathCounter(Dictionary<HexCoord, HexTileData> tiles, int maxPathsToFind = 10, int maxNodesToExplore = 200_000)
        {
            _tiles = tiles;
            _maxPathsToFind = maxPathsToFind;
            _maxNodesToExplore = maxNodesToExplore;
        }

        /// <summary>
        /// Ritorna il numero di percorsi trovati (capped a maxPathsToFind).
        /// 0 significa "mappa non beatable con questo food/seed".
        /// </summary>
        public int CountPaths(HexCoord start, int startingFood, HexCoord objective)
        {
            _nodesExplored = 0;
            var revealed = new HashSet<HexCoord> { start };
            return Explore(start, startingFood, objective, revealed);
        }

        private int Explore(HexCoord current, int food, HexCoord objective, HashSet<HexCoord> revealed)
        {
            if (current.Equals(objective)) return 1;
            if (_nodesExplored >= _maxNodesToExplore) return 0;
            _nodesExplored++;

            int found = 0;
            foreach (var kvp in _tiles)
            {
                var coord = kvp.Key;
                if (revealed.Contains(coord)) continue;

                int cost = coord.DistanceTo(current);
                if (cost > food) continue;

                revealed.Add(coord);
                int remainingFood = food - cost + kvp.Value.FoodReward;
                found += Explore(coord, remainingFood, objective, revealed);
                revealed.Remove(coord);

                if (found >= _maxPathsToFind) break;
            }

            return found;
        }
    }
}
