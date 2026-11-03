namespace hp55games.MapGame.Rules
{
    /// <summary>
    /// Dati fissi di un tipo di contenuto (03 §1-§5). Campi a zero/null quando non pertinenti.
    /// </summary>
    public sealed class ContentDef
    {
        public ContentKind Kind { get; set; }
        public ContentCategory Category { get; set; }
        /// <summary>Livello di mascheramento 1-6; 0 = nessun bordo (Start, Empty).</summary>
        public int MaskLevel { get; set; }
        /// <summary>Difesa (mostri, NPC combattibili).</summary>
        public int Defense { get; set; }
        public int Xp { get; set; }
        /// <summary>Monete date dal contenuto (uccisione mostro, forziere di monete).</summary>
        public int Coins { get; set; }
        /// <summary>Danno fisso (trappole).</summary>
        public int TrapDamage { get; set; }
        /// <summary>HP curati (arbusto, forziere di bacche). FullHeal per la fontana.</summary>
        public int Heal { get; set; }
        public bool FullHeal { get; set; }
        /// <summary>Oggetto dato (forziere, mini-boss arma).</summary>
        public string ItemId { get; set; }
        /// <summary>Chiave data (mini-boss chiave).</summary>
        public KeyKind GivesKey { get; set; }
        /// <summary>Chiave richiesta per aprire (forzieri d'argento/oro).</summary>
        public KeyKind RequiresKey { get; set; }
        /// <summary>True solo per il Boss.</summary>
        public bool IsObjective { get; set; }
        /// <summary>Bersaglio indizio a cui il contenuto appartiene, se c'è.</summary>
        public ClueTarget? ClueTarget { get; set; }
    }
}
