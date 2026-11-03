using System.Collections.Generic;

namespace hp55games.MapGame.Rules
{
    public enum DecisionKind { Npc = 0, LevelUp = 1, ClueLine = 2, Item = 3 }

    /// <summary>Opzione legale di una decisione (regola d'oro 4).</summary>
    public sealed class DecisionOption
    {
        public string Id { get; internal set; }
        /// <summary>Chiave di localizzazione dell'etichetta.</summary>
        public string LabelKey { get; internal set; }
        public bool Enabled { get; internal set; } = true;
        /// <summary>Chiave del motivo per cui è disabilitata (null se abilitata).</summary>
        public string DisabledReasonKey { get; internal set; }
        public int Price { get; internal set; }
        /// <summary>Per le decisioni di linea.</summary>
        public LineRef Line { get; internal set; }
        public string ItemId { get; internal set; }
    }

    /// <summary>Decisione pendente: il motore resta in attesa di Submit con un'opzione abilitata.</summary>
    public sealed class Decision
    {
        public DecisionKind Kind { get; internal set; }
        public List<DecisionOption> Options { get; } = new List<DecisionOption>();

        /// <summary>Npc: tile e tipo dell'NPC.</summary>
        public int TileIndex { get; internal set; } = -1;
        public ContentKind Npc { get; internal set; }

        /// <summary>ClueLine: bersaglio dell'indizio da posare.</summary>
        public ClueTarget ClueTarget { get; internal set; }

        /// <summary>Item: oggetto trovato e oggetto attuale nello slot.</summary>
        public string NewItemId { get; internal set; }
        public string CurrentItemId { get; internal set; }

        /// <summary>Le decisioni di livello e di linea non si possono chiudere senza scegliere.</summary>
        public bool CanClose => Kind == DecisionKind.Npc;

        public DecisionOption Find(string id)
        {
            foreach (var o in Options) if (o.Id == id) return o;
            return null;
        }
    }

    /// <summary>Id fissi delle opzioni.</summary>
    public static class OptionIds
    {
        public const string Attack = "attack";
        public const string Close = "close";
        public const string LevelAttack = "lvl_attack";
        public const string LevelCoins = "lvl_coins";
        public const string LevelMaxHp = "lvl_maxhp";
        public const string Equip = "equip";
        public const string Discard = "discard";

        public static string Line(LineRef l) => (l.IsRow ? "row:" : "col:") + l.Index;
    }
}
