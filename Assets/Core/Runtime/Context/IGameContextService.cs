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
        /// Usato dal costo movimento di MapGame. Stesso pattern di Lives: -1 = "sistema non
        /// in uso". Nessun MaxFood qui: il cap vive in SurvivalConfig e nel cap runtime di
        /// HexGridController, che gli upgrade dello shop possono alzare.
        /// </summary>
        int Food { get; set; }

        /// <summary>
        /// Chiavi di sessione (GDD, Resources — Chiavi Key DL4/DL5/DL6): risorse on/off
        /// raccolte rivelando la tile Key corrispondente, mai consumate dopo l'uso, valide
        /// per tutta la run. Una chiave DLn apre il Chest DL(n-3). Resettate da ResetRun.
        /// </summary>
        bool HasKeyDL4 { get; set; }

        /// <summary>Vedi HasKeyDL4.</summary>
        bool HasKeyDL5 { get; set; }

        /// <summary>Vedi HasKeyDL4.</summary>
        bool HasKeyDL6 { get; set; }

        /// <summary>
        /// Resets all run-related transient data (score, lives, food, keys, seed).
        /// Does NOT touch ProfileId or IsDebug.
        /// </summary>
        void ResetRun();
    }
}
