namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Knowledge axis: what the player knows about this tile.
    /// Clickability is a separate computed concept (see HexGridController.IsClickable).
    ///
    /// Sconosciuta  — no information; displayed as full cloud cover.
    /// Conosciuta   — position/terrain is known, content not yet resolved.
    ///                Covers both "adjacent to a Scoperta tile" and
    ///                "known via design/mission brief" (e.g. the End/objective tile).
    /// Scoperta     — content resolved; the player has interacted with this tile.
    /// </summary>
    //public enum TileState
    //{
    //    Sconosciuta, // ExplorationState.Unexplored && TileKnowledgeState.Unspotted
    //    Conosciuta, // ExplorationState.Unexplored && TileKnowledgeState.Unspotted
    //    Scoperta, // ExplorationState.Explored && TileKnowledgeState.Spotted
    //}
    
    public enum ExplorationState
    {
        Unexplored,
        Explored
    }

    public enum SpottingState
    {
        Unspotted,
        Spotted
    }
}
