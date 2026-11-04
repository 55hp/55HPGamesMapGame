using System.Collections.Generic;
using hp55games.MapGame.Rules;
using hp55games.Mobile.Core.Architecture;
using hp55games.Mobile.Core.Localization;

namespace hp55games.MapGame.Features.UI.Run
{
    /// <summary>
    /// Testi della partita v2: costruisce le chiavi (prefisso "run.") e le risolve con ILocalizationService.
    /// Nessun testo per il giocatore nel codice (regola 10): le stringhe stanno in localization_master.txt.
    /// </summary>
    public static class RunText
    {
        public static string T(string key, params object[] args)
        {
            if (!ServiceRegistry.TryResolve(out ILocalizationService loc) || loc == null) return key;
            return args == null || args.Length == 0 ? loc.Get(key) : loc.Get(key, args);
        }

        public static string Content(ContentKind k) => T("run.content." + Snake(k.ToString()));
        public static string Item(string id) => id == null ? T("run.item.none") : T("run.item." + id);

        /// <summary>D-050: Trinket posseduti, separati da virgola.</summary>
        public static string Trinkets(RunState s)
        {
            if (s.Trinkets.Count == 0) return T("run.item.none");
            var names = new List<string>();
            foreach (var t in s.Trinkets) names.Add(Item(t.Id));
            return string.Join(", ", names);
        }

        /// <summary>D-050: cosa hai in mano ora.</summary>
        public static string Hands(RunState s)
        {
            if (s.MainHand == null && s.OffHand == null) return T("run.item.none");
            if (s.OffHand == null) return Item(s.MainHand?.Id);
            return Item(s.MainHand?.Id) + " + " + Item(s.OffHand.Id);
        }
        public static string Offer(string id) => T("run.offer." + id);
        public static string Clue(ClueTarget t) => T("run.clue." + Snake(t.ToString()));
        public static string ClueShort(ClueTarget t) => T("run.clue_short." + Snake(t.ToString()));
        public static string Mission(MissionDef m) => m == null ? "" : T("run.mission." + m.Id);
        public static string Line(LineRef line, int height) => line.Label(height);

        /// <summary>Riga del registro per un evento, null se l'evento non va mostrato.</summary>
        public static string Describe(RuleEvent e, int height)
        {
            switch (e.Type)
            {
                case RuleEventType.MissionStarted: return T("run.log.mission", Clue(e.ClueTarget));
                case RuleEventType.EncounterWon: return T("run.log.encounter_won", Content(e.Kind), e.Attack, e.Defense);
                case RuleEventType.EncounterLost: return T("run.log.encounter_lost", Content(e.Kind), e.Attack, e.Defense, e.Amount);
                case RuleEventType.TrapTriggered: return T("run.log.trap", e.Amount);
                case RuleEventType.TrapDisarmed: return T("run.log.trap_disarmed");
                case RuleEventType.Healed: return T("run.log.healed", e.Amount);
                case RuleEventType.CoinsGained: return T("run.log.coins", e.Amount);
                case RuleEventType.XpGained: return T("run.log.xp", e.Amount);
                case RuleEventType.LineCompleted: return T("run.log.line", Line(e.Line, height));
                case RuleEventType.LevelUp: return T("run.log.level", e.Amount);
                case RuleEventType.ItemEquipped: return T("run.log.equipped", Item(e.ItemId));
                case RuleEventType.ItemDiscarded: return T("run.log.discarded", Item(e.ItemId));
                case RuleEventType.KeyObtained: return T(e.Key == KeyKind.Silver ? "run.log.key_silver" : "run.log.key_gold");
                case RuleEventType.ChestOpened: return T("run.log.chest");
                case RuleEventType.ChestVanished: return T("run.log.chest_vanished");
                case RuleEventType.NpcKilled: return T("run.log.npc_killed", Content(e.Kind));
                case RuleEventType.NpcFled: return T("run.log.npc_fled", Content(e.Kind), e.Amount);
                case RuleEventType.OfferBought: return T("run.log.bought", Offer(e.OfferId), e.Amount);
                case RuleEventType.ClueUsed: return T("run.log.clue", Clue(e.ClueTarget), Line(e.Line, height), e.Amount);
                case RuleEventType.ClueLost: return T("run.log.clue_lost");
                case RuleEventType.TileTypeShown: return T("run.log.lantern", Content(e.Kind));
                case RuleEventType.GameWon: return T("run.end.won");
                case RuleEventType.GameLost: return T(e.LossReason == LossReason.BossFled ? "run.end.boss_fled" : "run.end.dead");
                default: return null;
            }
        }

        private static string ScoutKey(int shape, string part)
        {
            string s = shape == ScoutShapes.Row ? "row" : shape == ScoutShapes.Column ? "col" : shape == ScoutShapes.Cluster ? "area" : "one";
            return "run.scout." + part + "_" + s;
        }

        public static string DecisionTitle(RulesEngine e, Decision d)
        {
            switch (d.Kind)
            {
                case DecisionKind.Npc: return Content(d.Npc);
                case DecisionKind.LevelUp: return T("run.levelup.title", e.State.Level);
                case DecisionKind.ClueLine: return T("run.clue.title", Clue(d.ClueTarget));
                case DecisionKind.Scout: return T(ScoutKey(d.ScoutShape, "title"));
                case DecisionKind.Item: return T("run.item.found", Item(d.NewItemId));
                default: return "";
            }
        }

        public static string DecisionBody(RulesEngine e, Decision d)
        {
            switch (d.Kind)
            {
                case DecisionKind.Npc: return T("run.npc.body");
                case DecisionKind.LevelUp: return T("run.levelup.body");
                case DecisionKind.ClueLine: return T("run.clue.body");
                case DecisionKind.Scout: return T(ScoutKey(d.ScoutShape, "body"));
                case DecisionKind.Item: return T("run.item.body", Hands(e.State));
                default: return "";
            }
        }

        public static string OptionLabel(RulesEngine e, Decision d, DecisionOption o)
        {
            var c = e.Config;
            switch (o.Id)
            {
                case OptionIds.Attack: return T("run.npc.attack");
                case OptionIds.Close: return T("run.npc.close");
                case OptionIds.LevelAttack: return T("run.levelup.attack", c.LevelUpAttack);
                case OptionIds.LevelCoins: return T("run.levelup.coins", c.LevelUpCoinBonus);
                case OptionIds.LevelMaxHp: return T("run.levelup.maxhp", c.LevelUpMaxHp);
                case OptionIds.Discard: return T("run.item.discard");
            }
            if (OptionIds.IsEquip(o.Id)) return T("run.item.equip");
            // Le offerte di indizi ricevono il bersaglio effettivo (D-038: per il Mercante è estratto all'apertura).
            return T("run.offer." + o.Id, Clue(o.ClueTarget));
        }

        /// <summary>Riga secondaria: prezzo, motivo di disabilitazione, rischio dell'attacco.</summary>
        public static string OptionDetail(RulesEngine e, Decision d, DecisionOption o)
        {
            var c = e.Config;
            if (o.Id == OptionIds.Attack)
            {
                // C1: l'esito è già deciso dal motore; lo mostriamo prima della scelta (P2).
                if (!o.PredictedWin) return T("run.npc.attack_lose", o.PredictedDamage);
                var npc = c.Npcs[d.Npc];
                string reward = npc.KillItemId != null ? Item(npc.KillItemId) : T("run.npc.reward_coins", npc.KillCoins);
                return T("run.npc.attack_win", reward);
            }
            if (o.Id == OptionIds.EquipAll) return T("run.item.equip_detail", Hands(e.State));
            if (OptionIds.IsEquip(o.Id)) return T("run.item.equip_detail", Item(o.ItemId));
            if (o.Price <= 0) return "";
            string price = T("run.npc.price", o.Price);
            if (o.Enabled) return price;
            string reason = o.DisabledReasonKey == "ui.npc.reason.already_bought" ? "run.npc.reason_bought"
                : o.DisabledReasonKey == "ui.npc.reason.already_owned" ? "run.npc.reason_owned" : "run.npc.reason_coins";
            return price + " · " + T(reason);
        }

        private static string Snake(string pascal)
        {
            var sb = new System.Text.StringBuilder(pascal.Length + 4);
            for (int i = 0; i < pascal.Length; i++)
            {
                char ch = pascal[i];
                if (char.IsUpper(ch) && i > 0) sb.Append('_');
                sb.Append(char.ToLowerInvariant(ch));
            }
            return sb.ToString();
        }
    }
}
