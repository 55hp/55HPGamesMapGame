namespace hp55games.Mobile.Core.Context
{
    /// <summary>
    /// Default implementation of IGameContextService.
    /// Simple in-memory container for the current session state.
    /// </summary>
    public sealed class GameContextService : IGameContextService
    {
        public string ProfileId      { get; set; } = "default";
        public string CurrentLevelId { get; set; }
        public int    CurrentRunSeed { get; set; }
        public bool   IsDebug        { get; set; }

        public int Score { get; set; }
        public int BestScore { get; set; }
        public int Lives { get; set; } = -1; // -1 = "no lives system" by default
        public int Food  { get; set; } = -1; // -1 = "no food system" by default
        public int Xp    { get; set; } = 0;
        public int Level { get; set; } = 1;  // il personaggio parte al livello 1

        public void ResetRun()
        {
            CurrentLevelId = null;
            CurrentRunSeed = 0;
            Score          = 0;
            Lives          = -1;
            Food           = -1;
            Xp             = 0;
            Level          = 1;
        }
    }
}
