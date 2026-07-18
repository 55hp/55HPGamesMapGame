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

        int Food { get; set; }

        /// <summary>
        /// Punti esperienza correnti del personaggio per questa run (0 = nessuno).
        /// Reintrodotto 2026-07-17. Guadagnati sconfiggendo Enemy (XP = DifficultyLevel del
        /// nemico), cappati a HexGridController.XpPerLevel; l'eccesso è perso. Il level up
        /// esplicito (HexGridController.TryLevelUp) li riporta a 0.
        /// </summary>
        int Xp { get; set; }

        /// <summary>
        /// Livello corrente del personaggio (parte da 1). Ogni level up: +1 e alza il cap
        /// runtime di HP (sempre) e di Cibo (ogni 4 livelli). Vedi HexGridController.
        /// </summary>
        int Level { get; set; }

        /// <summary>
        /// Resets all run-related transient data (score, lives, food, xp, character level, seed).
        /// Does NOT touch ProfileId or IsDebug.
        /// </summary>
        void ResetRun();
    }
}
