using System.Collections.Generic;

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

        /// <summary>D-050: mano 1 (un'arma a due mani sta qui e lascia vuota la mano 2).</summary>
        public ItemDef MainHand { get; internal set; }
        /// <summary>D-050: mano 2: un'arma a una mano o un oggetto da mano (Anello, Amuleto, Bussola).</summary>
        public ItemDef OffHand { get; internal set; }
        /// <summary>D-050: Trinket cumulabili, uno per tipo.</summary>
        public IReadOnlyList<ItemDef> Trinkets => _trinkets;
        internal readonly List<ItemDef> _trinkets = new List<ItemDef>();
        /// <summary>D-050: Attacco di un'arma in più oltre la migliore (dalla config).</summary>
        public int OffHandWeaponBonus { get; internal set; }
        public int OffHandWeaponMinBonus { get; internal set; }

        public bool HasSilverKey { get; internal set; }
        public bool HasGoldKey { get; internal set; }

        public RunStatus Status { get; internal set; }
        public LossReason LossReason { get; internal set; }
        public MissionDef Mission { get; internal set; }

        /// <summary>R-023 / D-050: base + mani + Fabbro + level up + missione.</summary>
        public int Attack => BaseAttack + HandsAttackOf(MainHand, OffHand, OffHandWeaponBonus, OffHandWeaponMinBonus) + TrinketsAttack
            + SmithBonus + LevelAttackBonus + MissionBonus;

        /// <summary>D-051: Attacco dei Trinket (Cote del Fabbro).</summary>
        public int TrinketsAttack { get { int a = 0; foreach (var t in _trinkets) a += t.AttackBonus; return a; } }

        /// <summary>
        /// D-050: l'arma migliore dà il suo bonus pieno, un'altra arma dà solo offHandWeaponBonus,
        /// un oggetto da mano dà il suo bonus. Pubblico per bot e UI (anteprime).
        /// </summary>
        public static int HandsAttackOf(ItemDef a, ItemDef b, int offHandWeaponBonus, int offHandWeaponMinBonus = 0)
        {
            int total = 0;
            ItemDef best = null, other = null;
            foreach (var it in new[] { a, b })
            {
                if (it == null) continue;
                if (it.Slot != ItemSlot.Weapon) { total += it.AttackBonus; continue; }
                if (best == null || it.AttackBonus > best.AttackBonus) { other = best; best = it; }
                else other = it;
            }
            if (best != null) total += best.AttackBonus;
            if (other != null && other.AttackBonus >= offHandWeaponMinBonus) total += offHandWeaponBonus;
            return total;
        }

        /// <summary>D-046: bonus di Attacco per aver raggiunto il bersaglio della missione.</summary>
        public int MissionBonus { get; internal set; }
        public bool MissionReached { get; internal set; }

        public bool HasKey(KeyKind key) =>
            key == KeyKind.None || (key == KeyKind.Silver && HasSilverKey) || (key == KeyKind.Gold && HasGoldKey);

        /// <summary>D-050: mani libere (un'arma a due mani le occupa entrambe).</summary>
        public int FreeHands =>
            MainHand != null && MainHand.TwoHanded ? 0 : (MainHand == null ? 1 : 0) + (OffHand == null ? 1 : 0);

        public bool HasItem(string id)
        {
            if (MainHand?.Id == id || OffHand?.Id == id) return true;
            foreach (var t in _trinkets) if (t.Id == id) return true;
            return false;
        }

        public bool HasEffect(ItemEffect fx)
        {
            if (fx == ItemEffect.None) return false;
            if (MainHand?.Effect == fx || OffHand?.Effect == fx) return true;
            foreach (var t in _trinkets) if (t.Effect == fx) return true;
            return false;
        }

        /// <summary>Per test e strumenti: mette un oggetto nel primo posto libero (o lo aggiunge ai Trinket).</summary>
        internal void Give(ItemDef item)
        {
            if (item.Slot == ItemSlot.Trinket) { if (!HasItem(item.Id)) _trinkets.Add(item); return; }
            if (item.TwoHanded) { MainHand = item; OffHand = null; }
            else if (MainHand == null) MainHand = item;
            else OffHand = item;
        }
    }
}
