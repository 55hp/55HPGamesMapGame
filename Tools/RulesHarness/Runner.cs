using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace hp55games.MapGame.RulesHarness
{
    /// <summary>
    /// Runner minimale per i test NUnit delle regole: [Test], [TestCase], [SetUp], [TearDown].
    /// Argomento opzionale: filtro sul nome completo del test. Exit code 0 = tutto verde.
    /// </summary>
    internal static class Runner
    {
        private static int Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--sim") return Simulate(args);
            if (args.Length > 0 && args[0] == "--compare") return Compare(args);
            if (args.Length > 0 && args[0] == "--npcdiag") return NpcDiag(args);
            string filter = args.Length > 0 ? args[0] : null;
            int passed = 0, failed = 0;
            var fixtures = typeof(Runner).Assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.GetMethods().Any(IsTest))
                .OrderBy(t => t.FullName);

            foreach (var type in fixtures)
            {
                var setUp = type.GetMethods().FirstOrDefault(m => m.IsDefined(typeof(SetUpAttribute), true));
                var tearDown = type.GetMethods().FirstOrDefault(m => m.IsDefined(typeof(TearDownAttribute), true));

                foreach (var method in type.GetMethods().Where(IsTest).OrderBy(m => m.Name))
                {
                    var cases = method.GetCustomAttributes<TestCaseAttribute>(true).Select(c => c.Arguments).ToList();
                    if (cases.Count == 0) cases.Add(Array.Empty<object>());

                    foreach (var arguments in cases)
                    {
                        string name = type.FullName + "." + method.Name +
                                      (arguments.Length > 0 ? "(" + string.Join(",", arguments) + ")" : "");
                        if (filter != null && name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;

                        object instance = Activator.CreateInstance(type);
                        try
                        {
                            setUp?.Invoke(instance, null);
                            try { method.Invoke(instance, arguments); }
                            finally { tearDown?.Invoke(instance, null); }
                            passed++;
                        }
                        catch (TargetInvocationException e)
                        {
                            var inner = e.InnerException;
                            if (inner is SuccessException) { passed++; continue; }
                            failed++;
                            Console.WriteLine("FAIL " + name);
                            Console.WriteLine("     " + (inner?.Message ?? e.Message).Trim().Replace("\n", "\n     "));
                        }
                    }
                }
            }

            Console.WriteLine($"Passati: {passed}  Falliti: {failed}");
            return failed == 0 ? 0 : 1;
        }

        /// <summary>Bot di bilanciamento (07 §3): --sim [partite] [csv]. Stampa un riepilogo e, se indicato, scrive il CSV.</summary>
        private static int Simulate(string[] args)
        {
            int games = args.Length > 1 ? int.Parse(args[1]) : 10000;
            string csv = args.Length > 2 ? args[2] : null;
            var config = hp55games.MapGame.Rules.RulesConfigDefaults.Level1();
            // Override opzionali: Nome=valore su proprietà int di RulesConfig (es. BaseAttack=1).
            for (int a = 3; a < args.Length; a++)
            {
                var kv = args[a].Split('=');
                var prop = typeof(hp55games.MapGame.Rules.RulesConfig).GetProperty(kv[0]);
                object value = prop.PropertyType.IsEnum ? Enum.Parse(prop.PropertyType, kv[1])
                        : prop.PropertyType == typeof(bool) ? (object)bool.Parse(kv[1])
                        : prop.PropertyType == typeof(System.Collections.Generic.List<int>)
                            ? (object)new System.Collections.Generic.List<int>(Array.ConvertAll(kv[1].Length == 0 ? new string[0] : kv[1].Split(','), int.Parse))
                        : (object)int.Parse(kv[1]);
                prop.SetValue(config, value);
                Console.WriteLine("override " + args[a]);
            }
            var lines = new System.Collections.Generic.List<string> { "policy,seed,mission,status,loss,level,attack,coins_spent,reveals" };
            foreach (var name in new[] { "random", "greedy" })
            {
                int won = 0, death = 0, fled = 0; long lvl = 0, atk = 0;
                var byMission = new System.Collections.Generic.Dictionary<string, int[]>();
                for (int i = 0; i < games; i++)
                {
                    ulong seed = (ulong)i;
                    hp55games.MapGame.Rules.IPlayerPolicy p = name == "random"
                        ? new hp55games.MapGame.Rules.RandomPolicy(seed + 99)
                        : (hp55games.MapGame.Rules.IPlayerPolicy)new hp55games.MapGame.Rules.GreedyPolicy(seed + 99);
                    var r = hp55games.MapGame.Rules.BalanceSimulator.Play(config, seed, p);
                    if (r.Status == hp55games.MapGame.Rules.RunStatus.Won) won++;
                    else if (r.Loss == hp55games.MapGame.Rules.LossReason.Death) death++;
                    else fled++;
                    lvl += r.Level; atk += r.AttackAtEnd;
                    if (!byMission.TryGetValue(r.Mission, out var m)) byMission[r.Mission] = m = new int[2];
                    m[1]++; if (r.Status == hp55games.MapGame.Rules.RunStatus.Won) m[0]++;
                    lines.Add($"{name},{r.Seed},{r.Mission},{r.Status},{r.Loss},{r.Level},{r.AttackAtEnd},{r.CoinsSpent},{r.Reveals}");
                }
                Console.WriteLine($"{name,-7} vittorie {100.0 * won / games:F1}%  morte {100.0 * death / games:F1}%  boss fuggito {100.0 * fled / games:F1}%  livello medio {(double)lvl / games:F2}  attacco medio a fine partita {(double)atk / games:F2}");
                foreach (var kv in byMission) Console.WriteLine($"        {kv.Key,-14} {100.0 * kv.Value[0] / kv.Value[1]:F1}%");
            }
            if (!string.IsNullOrEmpty(csv)) System.IO.File.WriteAllLines(csv, lines);
            return 0;
        }

        /// <summary>
        /// Confronto modalità × politiche: --compare [partite] [file.md] [etichetta] [Nome=valore ...].
        /// Stampa e (se indicato) scrive una tabella markdown di metriche da confrontare tra versioni.
        /// </summary>
        /// <summary>Diagnostica: cosa fa il bot greedy a ogni NPC (tipo, monete, Attacco, scelta).</summary>
        private sealed class DiagPolicy : hp55games.MapGame.Rules.IPlayerPolicy
        {
            private readonly hp55games.MapGame.Rules.IPlayerPolicy _inner;
            public readonly System.Collections.Generic.Dictionary<string, int> Counts = new System.Collections.Generic.Dictionary<string, int>();
            public double Coins; public int N;
            public DiagPolicy(hp55games.MapGame.Rules.IPlayerPolicy inner) { _inner = inner; }
            public int ChooseTile(hp55games.MapGame.Rules.RulesEngine e) => _inner.ChooseTile(e);
            public string ChooseOption(hp55games.MapGame.Rules.RulesEngine e, hp55games.MapGame.Rules.Decision d)
            {
                var c = _inner.ChooseOption(e, d);
                if (d.Kind == hp55games.MapGame.Rules.DecisionKind.Npc)
                {
                    string k = d.Npc + " -> " + c;
                    Counts[k] = Counts.TryGetValue(k, out var v) ? v + 1 : 1;
                    Coins += e.State.Coins; N++;
                }
                return c;
            }
        }

        private static int NpcDiag(string[] args)
        {
            int games = int.Parse(args[1]);
            var config = hp55games.MapGame.Rules.RulesConfigDefaults.Level1();
            config.Mode = (hp55games.MapGame.Rules.GameMode)Enum.Parse(typeof(hp55games.MapGame.Rules.GameMode), args[2]);
            var diag = new DiagPolicy(null);
            var all = new System.Collections.Generic.Dictionary<string, int>();
            double coins = 0; int n = 0;
            for (int i = 0; i < games; i++)
            {
                var p = new DiagPolicy(new hp55games.MapGame.Rules.GreedyPolicy((ulong)i + 99, "default"));
                hp55games.MapGame.Rules.BalanceSimulator.Play(config, (ulong)i, p);
                foreach (var kv in p.Counts) all[kv.Key] = (all.TryGetValue(kv.Key, out var v) ? v : 0) + kv.Value;
                coins += p.Coins; n += p.N;
            }
            Console.WriteLine($"Monete medie all'apertura di un NPC: {coins / Math.Max(1, n):F1}");
            var keys = new System.Collections.Generic.List<string>(all.Keys); keys.Sort();
            foreach (var k in keys) Console.WriteLine($"{k}: {all[k] / (double)games:F2} a partita");
            return 0;
        }

        private static int Compare(string[] args)
        {
            int games = args.Length > 1 ? int.Parse(args[1]) : 5000;
            string md = args.Length > 2 && args[2] != "-" ? args[2] : null;
            string label = args.Length > 3 ? args[3] : "senza etichetta";
            var overrides = new System.Collections.Generic.List<string>();
            for (int a = 4; a < args.Length; a++) overrides.Add(args[a]);

            var modes = new System.Collections.Generic.List<hp55games.MapGame.Rules.GameMode> { hp55games.MapGame.Rules.GameMode.Open, hp55games.MapGame.Rules.GameMode.Fog, hp55games.MapGame.Rules.GameMode.FogAdjacent };
            var botOv = overrides.Find(x => x.StartsWith("BotLevelUp="));
            string botLevelUp = "default";
            if (botOv != null) { overrides.Remove(botOv); botLevelUp = botOv.Substring("BotLevelUp=".Length); }
            var shopOv = overrides.Find(x => x.StartsWith("BotShop="));
            bool shop = false;
            if (shopOv != null) { overrides.Remove(shopOv); shop = bool.Parse(shopOv.Substring("BotShop=".Length)); }
            var modeOv = overrides.Find(x => x.StartsWith("Mode="));
            if (modeOv != null) { overrides.Remove(modeOv); modes.Clear(); modes.Add((hp55games.MapGame.Rules.GameMode)Enum.Parse(typeof(hp55games.MapGame.Rules.GameMode), modeOv.Substring(5))); }
            var policies = new[] { "random", "greedy", "clue" };
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"| Modalità | Bot | Vittorie | Morte | Boss fuggito | Livello | Attacco fine | HP fine (vittorie) | Danni | Tocchi | Tocchi ciechi | Mosse legali medie | Scelte con mossa sicura | Uccisioni | Mini-boss | NPC aperti | Acquisti | Indizi extra ricevuti | Indizi usati (con i 2 di partenza) | Indizi persi |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (var mode in modes)
            foreach (var name in policies)
            {
                var config = hp55games.MapGame.Rules.RulesConfigDefaults.Level1();
                config.Mode = mode;
                foreach (var ov in overrides)
                {
                    var kv = ov.Split('=');
                    var prop = typeof(hp55games.MapGame.Rules.RulesConfig).GetProperty(kv[0]);
                    object value = prop.PropertyType.IsEnum ? Enum.Parse(prop.PropertyType, kv[1])
                        : prop.PropertyType == typeof(bool) ? (object)bool.Parse(kv[1])
                        : prop.PropertyType == typeof(System.Collections.Generic.List<int>)
                            ? (object)new System.Collections.Generic.List<int>(Array.ConvertAll(kv[1].Length == 0 ? new string[0] : kv[1].Split(','), int.Parse))
                        : (object)int.Parse(kv[1]);
                    prop.SetValue(config, value);
                }
                config.RepriceOffers();
                int won = 0, death = 0, fled = 0;
                double lvl = 0, atk = 0, hpWin = 0, dmg = 0, rev = 0, blind = 0, legal = 0, safe = 0, kills = 0, mb = 0, npc = 0, buy = 0, cr = 0, cu = 0, cl = 0;
                for (int i = 0; i < games; i++)
                {
                    ulong seed = (ulong)i;
                    hp55games.MapGame.Rules.IPlayerPolicy p =
                        name == "random" ? new hp55games.MapGame.Rules.RandomPolicy(seed + 99)
                        : name == "greedy" ? (hp55games.MapGame.Rules.IPlayerPolicy)new hp55games.MapGame.Rules.GreedyPolicy(seed + 99, botLevelUp, shop)
                        : new hp55games.MapGame.Rules.ClueAwarePolicy(seed + 99, botLevelUp, shop);
                    var r = hp55games.MapGame.Rules.BalanceSimulator.Play(config, seed, p);
                    if (r.Status == hp55games.MapGame.Rules.RunStatus.Won) { won++; hpWin += r.HpAtEnd; }
                    else if (r.Loss == hp55games.MapGame.Rules.LossReason.Death) death++;
                    else fled++;
                    lvl += r.Level; atk += r.AttackAtEnd; dmg += r.DamageTaken; rev += r.Reveals; blind += r.BlindClicks;
                    legal += r.TileChoices == 0 ? 0 : (double)r.LegalChoicesSum / r.TileChoices;
                    safe += r.TileChoices == 0 ? 0 : (double)r.ChoicesWithSafeMove / r.TileChoices;
                    kills += r.Kills; mb += r.MiniBossKills; npc += r.NpcsOpened; buy += r.OffersBought;
                    cr += r.CluesReceived; cu += r.CluesUsed; cl += r.CluesLost;
                }
                double g = games;
                string P(double x) => (100.0 * x / g).ToString("F1") + "%";
                string A(double x) => (x / g).ToString("F2");
                string modeName = mode == hp55games.MapGame.Rules.GameMode.Open ? "Classica" : mode == hp55games.MapGame.Rules.GameMode.Fog ? "Nebbia" : "Esplorazione";
                string row = $"| {modeName} | {name} | {P(won)} | {P(death)} | {P(fled)} | {A(lvl)} | {A(atk)} | {(won == 0 ? "-" : (hpWin / won).ToString("F2"))} | {A(dmg)} | {A(rev)} | {A(blind)} | {A(legal)} | {(100.0 * safe / g):F1}% | {A(kills)} | {A(mb)} | {A(npc)} | {A(buy)} | {A(cr)} | {A(cu)} | {A(cl)} |";
                sb.AppendLine(row);
                Console.WriteLine(row);
            }
            if (md != null)
            {
                var header = $"## {DateTime.UtcNow:yyyy-MM-dd} — {label}\n\nPartite per riga: {games} (seed 0..{games - 1}). Override: {(overrides.Count == 0 ? "nessuno" : string.Join(", ", overrides))}. Bot level up: {botLevelUp}. Bot compra prima di attaccare: {(shop ? "sì" : "no")}.\n\n";
                System.IO.File.AppendAllText(md, header + sb + "\n");
            }
            return 0;
        }

        private static bool IsTest(MethodInfo m) =>
            m.IsDefined(typeof(TestAttribute), true) || m.IsDefined(typeof(TestCaseAttribute), true);
    }
}
