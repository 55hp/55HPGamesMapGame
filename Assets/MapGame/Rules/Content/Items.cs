namespace hp55games.MapGame.Rules
{
    /// <summary>
    /// D-050: Weapon e Hand vanno nelle due mani (un'arma a due mani le occupa entrambe); i Trinket sono cumulabili.
    /// </summary>
    public enum ItemSlot { Weapon = 0, Hand = 1, Trinket = 2 }

    /// <summary>Effetto passivo di un oggetto (03 §6).</summary>
    public enum ItemEffect { None = 0, DisarmTraps = 1, Lantern = 2, Compass = 3, Purse = 4, MapGold = 5, MapSilver = 6 } // D-049 Bussola, Borsa; D-051 Mappe

    /// <summary>Oggetto equipaggiabile. Il bonus Attacco di un'arma è il suo livello.</summary>
    public sealed class ItemDef
    {
        public string Id { get; }
        public ItemSlot Slot { get; }
        public int AttackBonus { get; }
        public ItemEffect Effect { get; }
        /// <summary>D-050: arma a due mani (occupa anche la mano 2).</summary>
        public bool TwoHanded { get; }

        public ItemDef(string id, ItemSlot slot, int attackBonus, ItemEffect effect = ItemEffect.None, bool twoHanded = false)
        {
            Id = id; Slot = slot; AttackBonus = attackBonus; Effect = effect; TwoHanded = twoHanded && slot == ItemSlot.Weapon;
        }

        public bool InHand => Slot != ItemSlot.Trinket;
        public int HandsNeeded => TwoHanded ? 2 : 1;

        /// <summary>Chiave di localizzazione del nome.</summary>
        public string NameKey => "item." + Id;
    }
}
