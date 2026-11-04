namespace hp55games.MapGame.Rules
{
    public enum RunStatus { Playing = 0, Won = 1, Lost = 2 }

    public enum LossReason { None = 0, Death = 1, BossFled = 2 }

    /// <summary>
    /// Stato di run v2 (01_architettura §4). Lo modifica solo il RulesEngine; la presentazione lo legge.
    /// </summary>
    public sealed class RunState
    {
        public int Hp { get; internal set; }
        public int MaxHp { get; internal set; }
        public int Coins { get; internal set; }
        public int Xp { get; internal set; }
        /// <summary>Livello del personaggio, da 1.</summary>
        public int Level { get; internal set; } = 1;
        /// <summary>Monete in più a ogni evento di guadagno (R-038).</summary>
        public int CoinBonusPerGain { get; internal set; }
        /// <summary>Attacco ottenuto dai level up (R-042).</summary>
        public int LevelAttackBonus { get; internal set; }
        /// <summary>Attacco del Fabbro (R-029).</summary>
        public int SmithBonus { get; internal set; }
        public bool SmithUpgradeBought { get; internal set; }
        public int BaseAttack { get; internal set; }

        public ItemDef Weapon { get; internal set; }
        public ItemDef Ring { get; internal set; }
        public ItemDef Trinket { get; internal set; }

        public bool HasSilverKey { get; internal set; }
        public bool HasGoldKey { get; internal set; }

        public RunStatus Status { get; internal set; }
        public LossReason LossReason { get; internal set; }
        public MissionDef Mission { get; internal set; }

        /// <summary>R-023: base + Arma + Anello + Trinket + Fabbro + level up.</summary>
        public int Attack =>
            BaseAttack + (Weapon?.AttackBonus ?? 0) + (Ring?.AttackBonus ?? 0) + (Trinket?.AttackBonus ?? 0)
            + SmithBonus + LevelAttackBonus + MissionBonus;

        /// <summary>D-046: bonus di Attacco per aver raggiunto il bersaglio della missione.</summary>
        public int MissionBonus { get; internal set; }
        public bool MissionReached { get; internal set; }

        public bool HasKey(KeyKind key) =>
            key == KeyKind.None || (key == KeyKind.Silver && HasSilverKey) || (key == KeyKind.Gold && HasGoldKey);

        public ItemDef Equipped(ItemSlot slot) =>
            slot == ItemSlot.Weapon ? Weapon : slot == ItemSlot.Ring ? Ring : Trinket;

        public bool HasEffect(ItemEffect fx) => Trinket != null && Trinket.Effect == fx;

        internal void SetEquipped(ItemSlot slot, ItemDef item)
        {
            if (slot == ItemSlot.Weapon) Weapon = item;
            else if (slot == ItemSlot.Ring) Ring = item;
            else Trinket = item;
        }
    }
}
