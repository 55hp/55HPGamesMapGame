using System.Collections.Generic;

namespace hp55games.MapGame.Rules
{
    public enum DecisionKind { Npc = 0, LevelUp = 1, ClueLine = 2, Item = 3, Scout = 4 }

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
        /// <summary>D-054: pezzi disponibili da mostrare (-1 = infinito, 0 = non mostrare).</summary>
        public int Stock { get; internal set; }
        /// <summary>Per le decisioni di linea.</summary>
        public LineRef Line { get; internal set; }
        public string ItemId { get; internal set; }
        /// <summary>Attacco a un NPC: esito già noto (combattimento deterministico, R-063). Per mostrarlo prima della scelta (C1).</summary>
        public bool PredictedWin { get; internal set; }
        /// <summary>Offerta di indizi: bersaglio effettivo (fisso o estratto, D-038).</summary>
        public ClueTarget ClueTarget { get; internal set; }
        public int PredictedDamage { get; internal set; }
        /// <summary>Decisione Scout: tile centrale della Ricognizione.</summary>
        public int TileIndex { get; internal set; } = -1;
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

        /// <summary>Scout (D-043/D-045): forma della Ricognizione, vedi <see cref="ScoutShapes"/>.</summary>
        public int ScoutShape { get; internal set; }

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

    /// <summary>Forme della Ricognizione (D-043, D-045). Valgono anche come NpcOfferDef.Amount delle offerte Scout.</summary>
    public static class ScoutShapes
    {
        public const int Single = 0;   // una tile
        public const int Cluster = 1;  // tile + 6 vicine
        public const int Row = 2;      // riga intera della tile scelta
        public const int Column = 3;   // colonna intera della tile scelta
        public static bool IsLine(int shape) => shape == Row || shape == Column;
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
        /// <summary>D-050: equipaggia l'oggetto nuovo lasciando tutto ciò che hai in mano (arma a due mani).</summary>
        public const string EquipAll = "equip:all";
        /// <summary>D-050: equipaggia l'oggetto nuovo lasciando quello indicato.</summary>
        public static string EquipReplacing(string heldItemId) => "equip:" + heldItemId;
        public static bool IsEquip(string id) => id != null && id.StartsWith("equip");

        public static string Line(LineRef l) => (l.IsRow ? "row:" : "col:") + l.Index;
        public static string Tile(int index) => "tile:" + index;
    }
}
