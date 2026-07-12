namespace hp55games.Mobile.Core.Context
{
    /// <summary>
    /// Lightweight runtime game context.
    /// Stores current profile, run and level data for this session.
    /// Not responsible for persistence (that's SaveService's job).
    /// </summary>
    public interface IGameContextService
    {
        /// <summary>Current logical profile id (e.g. "default", "profile_1").</summary>
        string ProfileId { get; set; }

        /// <summary>Current level identifier (scene id, map id, etc.).</summary>
        string CurrentLevelId { get; set; }

        /// <summary>Current run seed (for roguelike / procedural runs).</summary>
        int CurrentRunSeed { get; set; }

        /// <summary>True if the game is running in debug/developer mode.</summary>
        bool IsDebug { get; set; }

        /// <summary>Current score for this run (optional, can be 0 if unused).</summary>
        int Score { get; set; }

        /// <summary>Best score (optional, can be 0 if unused).</summary>
        int BestScore { get; set; }

        /// <summary>Current lives for this run (optional, can be 0 or -1 if unused).</summary>
        int Lives { get; set; }

        /// <summary>
        /// Current food/hunger stock for this run (optional, can be 0 or -1 if unused).
        /// Aggiunto 2026-07-10 per il costo movimento di MapGame (1 cibo per click, HP se
        /// il cibo è a 0). Stesso pattern di Lives: -1 = "sistema non in uso" di default.
        /// Nessun MaxFood qui, stessa scelta già fatta per Lives/MaxHp: il massimo resta
        /// locale a chi possiede la regola di gioco (in MapGame, HexGridController._maxFood).
        /// </summary>
        int Food { get; set; }

        /// <summary>
        /// Resets all run-related transient data (score, lives, food, level, seed).
        /// Does NOT touch ProfileId or IsDebug.
        /// </summary>
        void ResetRun();
    }
}
