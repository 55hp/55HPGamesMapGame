namespace hp55games.MapGame.Rules
{
    public enum ItemSlot { Weapon = 0, Ring = 1, Trinket = 2 }

    /// <summary>Effetto passivo di un Trinket (03 §6).</summary>
    public enum ItemEffect { None = 0, DisarmTraps = 1, Lantern = 2 }

    /// <summary>Oggetto equipaggiabile. Il bonus Attacco di un'arma è il suo livello.</summary>
    public sealed class ItemDef
    {
        public string Id { get; }
        public ItemSlot Slot { get; }
        public int AttackBonus { get; }
        public ItemEffect Effect { get; }

        public ItemDef(string id, ItemSlot slot, int attackBonus, ItemEffect effect = ItemEffect.None)
        {
            Id = id; Slot = slot; AttackBonus = attackBonus; Effect = effect;
        }

        /// <summary>Chiave di localizzazione del nome.</summary>
        public string NameKey => "item." + Id;
    }
}
