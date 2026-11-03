using System.Collections.Generic;
using System.Linq;

namespace hp55games.MapGame.Rules.Tests
{
    internal static class RulesTestUtil
    {
        public static RulesConfig Config() => RulesConfigDefaults.Level1();

        /// <summary>Motore avviato, con gli indizi di partenza già posati sulle prime linee libere.</summary>
        public static RulesEngine Started(ulong seed = 1, RulesConfig config = null)
        {
            var e = new RulesEngine(config ?? Config(), seed);
            e.Start();
            ResolveAll(e);
            return e;
        }

        /// <summary>Risponde a tutte le decisioni pendenti con la prima opzione abilitata.</summary>
        public static void ResolveAll(RulesEngine e)
        {
            int guard = 0;
            while (e.CurrentDecision != null && guard++ < 100)
            {
                var d = e.CurrentDecision;
                var opt = d.Kind == DecisionKind.Npc ? d.Find(OptionIds.Close) : d.Options.First(o => o.Enabled);
                e.Submit(opt.Id);
            }
        }

        public static int Find(RulesEngine e, ContentKind kind)
        {
            for (int i = 0; i < e.Layout.Tiles.Length; i++)
                if (e.Layout.Tiles[i] == kind && !e.IsRevealed(i)) return i;
            return -1;
        }

        public static List<RuleEventType> Types(IReadOnlyList<RuleEvent> evs) => evs.Select(x => x.Type).ToList();

        public static RuleEvent First(IReadOnlyList<RuleEvent> evs, RuleEventType t) => evs.FirstOrDefault(x => x.Type == t);

        /// <summary>Imposta l'Attacco a un valore esatto tramite il bonus di livello.</summary>
        public static void SetAttack(RulesEngine e, int attack)
        {
            e.State.LevelAttackBonus = 0;
            e.State.LevelAttackBonus = attack - e.State.Attack;
        }
    }
}
