using System.Collections.Generic;

namespace hp55games.MapGame.Rules
{
    public enum NpcOfferEffect
    {
        Heal = 0,          // Amount HP
        FullHeal = 1,
        GiveItem = 2,      // ItemId
        SmithUpgrade = 3,  // +Amount Attacco permanente, una volta per run
        Clues = 4,         // Amount indizi su ClueTarget
    }

    public sealed class NpcOfferDef
    {
        public string Id { get; set; }
        public int Price { get; set; }
        public NpcOfferEffect Effect { get; set; }
        public int Amount { get; set; }
        public string ItemId { get; set; }
        public ClueTarget ClueTarget { get; set; }
        /// <summary>Acquistabile una sola volta per run (Fabbro).</summary>
        public bool OncePerRun { get; set; }

        public string NameKey => "offer." + Id;
    }

    public sealed class NpcDef
    {
        public ContentKind Kind { get; set; }
        public bool Fightable { get; set; }
        /// <summary>Premio se ucciso: un oggetto oppure monete.</summary>
        public string KillItemId { get; set; }
        public int KillCoins { get; set; }
        public List<NpcOfferDef> Offers { get; set; } = new List<NpcOfferDef>();
    }

    public sealed class MissionDef
    {
        public string Id { get; set; }
        public ClueTarget Target { get; set; }
        public int StartClues { get; set; }

        public string NameKey => "mission." + Id;
    }

    public struct DistanceWeight
    {
        public int Distance;
        public int Weight;
        public DistanceWeight(int distance, int weight) { Distance = distance; Weight = weight; }
    }
}
