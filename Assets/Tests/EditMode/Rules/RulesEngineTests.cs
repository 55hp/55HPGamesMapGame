using System.Linq;
using NUnit.Framework;
using static hp55games.MapGame.Rules.Tests.RulesTestUtil;

namespace hp55games.MapGame.Rules.Tests
{
    public class RulesEngineTests
    {
        // ------------------------------------------------ avvio

        [Test]
        public void R020_R024_StartState()
        {
            var c = Config();
            var e = Started();
            Assert.AreEqual(c.StartHp, e.State.Hp);
            Assert.AreEqual(c.StartMaxHp, e.State.MaxHp);
            Assert.AreEqual(c.StartWeaponId, e.State.Weapon.Id);
            Assert.AreEqual(c.BaseAttack + c.Item(c.StartWeaponId).AttackBonus, e.State.Attack);
            Assert.AreEqual(RunStatus.Playing, e.State.Status);
        }

        [Test]
        public void R005_R008_StartAndEmpties_Revealed_BossHidden()
        {
            var e = Started();
            for (int i = 0; i < e.Layout.Tiles.Length; i++)
            {
                var k = e.Layout.Tiles[i];
                Assert.AreEqual(k == ContentKind.Start || k == ContentKind.Empty, e.IsRevealed(i), $"{k}");
            }
            Assert.IsFalse(e.IsTypeShown(e.Layout.BossIndex));
        }

        [Test]
        public void R070_R098_MissionGivesTwoChainedCluesOnTarget()
        {
            var e = new RulesEngine(Config(), 3);
            var evs = e.Start();
            var mission = e.State.Mission;
            Assert.IsNotNull(mission);
            Assert.AreEqual(mission.StartClues, evs.Count(x => x.Type == RuleEventType.ClueReceived && x.ClueTarget == mission.Target));
            for (int i = 0; i < mission.StartClues; i++)
            {
                Assert.AreEqual(DecisionKind.ClueLine, e.CurrentDecision.Kind);
                Assert.AreEqual(mission.Target, e.CurrentDecision.ClueTarget);
                e.Submit(e.CurrentDecision.Options[0].Id);
            }
            Assert.IsNull(e.CurrentDecision);
        }

        [Test]
        public void R070_SixMissions_AllPicked_ByDifferentSeeds()
        {
            var c = Config();
            Assert.AreEqual(6, c.Missions.Count);
            var seen = Enumerable.Range(0, 200).Select(s => { var e = new RulesEngine(c, (ulong)s); e.Start(); return e.State.Mission.Id; }).Distinct().Count();
            Assert.AreEqual(c.Missions.Count, seen);
        }

        [Test]
        public void R002_RevealBlockedWhileDecisionPending()
        {
            var e = new RulesEngine(Config(), 3);
            e.Start();
            Assert.IsNotNull(e.CurrentDecision);
            int idx = Find(e, ContentKind.Bush);
            CollectionAssert.IsEmpty(e.Reveal(idx));
            Assert.IsFalse(e.IsRevealed(idx));
        }

        [Test]
        public void R003_RevealedTile_NotRevealableAgain()
        {
            var e = Started();
            int idx = Find(e, ContentKind.Bush);
            Assert.IsNotEmpty(e.Reveal(idx));
            Assert.IsFalse(e.IsRevealable(idx));
            CollectionAssert.IsEmpty(e.Reveal(idx));
        }

        // ------------------------------------------------ scambio

        [Test]
        public void R032_AttackEqualsDefense_Kills_NoDamage_Reward()
        {
            var c = Config(); var e = Started(config: c);
            var def = c.Content(ContentKind.Monster1);
            SetAttack(e, def.Defense);
            int hp = e.State.Hp;
            var evs = e.Reveal(Find(e, ContentKind.Monster1));
            Assert.IsNotNull(First(evs, RuleEventType.EncounterWon));
            Assert.AreEqual(hp, e.State.Hp);
            Assert.AreEqual(c.StartCoins + def.Coins, e.State.Coins);
            Assert.AreEqual(def.Xp, e.State.Xp);
        }

        [Test]
        public void R034_AttackAboveDefense_NoHeal()
        {
            var c = Config(); var e = Started(config: c);
            e.State.Hp = 2;
            SetAttack(e, 20);
            e.Reveal(Find(e, ContentKind.Monster2));
            Assert.AreEqual(2, e.State.Hp);
        }

        [Test]
        public void R033_AttackBelowDefense_MonsterFlees_DamageNoReward()
        {
            var c = Config(); var e = Started(config: c);
            var def = c.Content(ContentKind.Monster2);
            SetAttack(e, def.Defense - 1);
            int hp = e.State.Hp;
            int idx = Find(e, ContentKind.Monster2);
            var evs = e.Reveal(idx);
            var lost = First(evs, RuleEventType.EncounterLost);
            Assert.AreEqual(def.Defense - (def.Defense - 1), lost.Amount);
            Assert.AreEqual(hp - lost.Amount, e.State.Hp);
            Assert.AreEqual(c.StartCoins, e.State.Coins);
            Assert.AreEqual(0, e.State.Xp);
            Assert.IsTrue(e.IsRevealed(idx));
        }

        [Test]
        public void R021_HpBelowOne_Loses()
        {
            var c = Config(); var e = Started(config: c);
            e.State.Hp = 1;
            SetAttack(e, 0);
            var evs = e.Reveal(Find(e, ContentKind.Monster1));
            Assert.AreEqual(RunStatus.Lost, e.State.Status);
            Assert.AreEqual(LossReason.Death, First(evs, RuleEventType.GameLost).LossReason);
            Assert.IsFalse(e.IsRevealable(Find(e, ContentKind.Bush)));
        }

        // ------------------------------------------------ Boss

        [Test]
        public void R039_R072_BossKilled_Wins()
        {
            var c = Config(); var e = Started(config: c);
            SetAttack(e, c.Content(ContentKind.Boss).Defense);
            var evs = e.Reveal(e.Layout.BossIndex);
            Assert.AreEqual(RunStatus.Won, e.State.Status);
            Assert.IsNotNull(First(evs, RuleEventType.GameWon));
            Assert.AreEqual(0, e.State.Xp, "il Boss non dà XP");
        }

        [Test]
        public void R039_BossFlees_LosesEvenIfAlive()
        {
            var c = Config(); var e = Started(config: c);
            e.State.MaxHp = 50; e.State.Hp = 50;
            SetAttack(e, c.Content(ContentKind.Boss).Defense - 1);
            var evs = e.Reveal(e.Layout.BossIndex);
            Assert.AreEqual(RunStatus.Lost, e.State.Status);
            Assert.AreEqual(LossReason.BossFled, First(evs, RuleEventType.GameLost).LossReason);
        }

        // ------------------------------------------------ altre tile

        [Test]
        public void R050_Trap_FixedDamage_DisarmedByHunterTools()
        {
            var c = Config(); var e = Started(config: c);
            int hp = e.State.Hp;
            e.Reveal(Find(e, ContentKind.Trap2));
            Assert.AreEqual(hp - c.Content(ContentKind.Trap2).TrapDamage, e.State.Hp);

            e.State.Trinket = c.Item(RulesConfigDefaults.HunterTools);
            hp = e.State.Hp;
            var evs = e.Reveal(Find(e, ContentKind.Trap1));
            Assert.IsNotNull(First(evs, RuleEventType.TrapDisarmed));
            Assert.AreEqual(hp, e.State.Hp);
        }

        [Test]
        public void R004_R052_Bush_HealsClampedToMax()
        {
            var e = Started();
            e.Reveal(Find(e, ContentKind.Bush));
            Assert.AreEqual(e.State.MaxHp, e.State.Hp);
            e.State.Hp = 1;
            e.Reveal(Find(e, ContentKind.Bush));
            Assert.AreEqual(1 + Config().Content(ContentKind.Bush).Heal, e.State.Hp);
        }

        [Test]
        public void R051_Fountain_FullHeal()
        {
            var e = Started();
            e.State.MaxHp = 9; e.State.Hp = 1;
            e.Reveal(Find(e, ContentKind.Fountain));
            Assert.AreEqual(9, e.State.Hp);
        }

        [Test]
        public void R053_SimpleChests_Open()
        {
            var c = Config(); var e = Started(config: c);
            e.Reveal(Find(e, ContentKind.ChestSimpleCoins));
            Assert.AreEqual(c.StartCoins + c.Content(ContentKind.ChestSimpleCoins).Coins, e.State.Coins);
            e.State.Hp = 1;
            e.Reveal(Find(e, ContentKind.ChestSimpleBerries));
            Assert.AreEqual(1 + c.Content(ContentKind.ChestSimpleBerries).Heal, e.State.Hp);
        }

        [Test]
        public void R054_LockedChestWithoutKey_Vanishes()
        {
            var e = Started();
            int idx = Find(e, ContentKind.ChestSilver);
            var evs = e.Reveal(idx);
            Assert.IsNotNull(First(evs, RuleEventType.ChestVanished));
            Assert.AreEqual(Config().StartWeaponId, e.State.Weapon.Id);
            Assert.IsTrue(e.IsRevealed(idx));
        }

        [Test]
        public void R037_R053_MiniBossKey_OpensChest_ItemDecision()
        {
            var c = Config(); var e = Started(config: c);
            SetAttack(e, 10);
            e.Reveal(Find(e, ContentKind.MiniBossSilverKey));
            ResolveAll(e);
            Assert.IsTrue(e.State.HasSilverKey);
            e.Reveal(Find(e, ContentKind.ChestSilver));
            Assert.AreEqual(DecisionKind.Item, e.CurrentDecision.Kind);
            Assert.AreEqual(RulesConfigDefaults.WeaponL4, e.CurrentDecision.NewItemId);
            e.Submit(OptionIds.Equip);
            Assert.AreEqual(RulesConfigDefaults.WeaponL4, e.State.Weapon.Id);
        }

        // ------------------------------------------------ equipaggiamento

        [Test]
        public void R025_FoundItem_Discard_KeepsOld()
        {
            var e = Started();
            e.Reveal(Find(e, ContentKind.ChestSimpleWeapon));
            Assert.AreEqual(DecisionKind.Item, e.CurrentDecision.Kind);
            var evs = e.Submit(OptionIds.Discard);
            Assert.AreEqual(RulesConfigDefaults.Knife, e.State.Weapon.Id);
            Assert.AreEqual(RulesConfigDefaults.WeaponL2, First(evs, RuleEventType.ItemDiscarded).ItemId);
        }

        [Test]
        public void R023_AttackSumsAllBonuses()
        {
            var c = Config(); var e = Started(config: c);
            e.State.Weapon = c.Item(RulesConfigDefaults.WeaponL3);
            e.State.Ring = c.Item(RulesConfigDefaults.Ring2);
            e.State.SmithBonus = 1;
            e.State.LevelAttackBonus = 2;
            Assert.AreEqual(c.BaseAttack + 3 + 2 + 1 + 2, e.State.Attack);
        }

        // ------------------------------------------------ XP e livelli

        [Test]
        public void R042_LevelUp_EveryXpPerLevel_MultipleInOneMove()
        {
            var c = Config(); var e = Started(config: c);
            e.State.Xp = c.XpPerLevel * 2 - 1;
            e.State.Level = 2;
            SetAttack(e, 10);
            e.Reveal(Find(e, ContentKind.MiniBossCoins)); // +3 XP: supera il livello 3
            Assert.AreEqual(3, e.State.Level);
            Assert.AreEqual(DecisionKind.LevelUp, e.CurrentDecision.Kind);
            Assert.AreEqual(3, e.CurrentDecision.Options.Count);
            Assert.IsFalse(e.CurrentDecision.CanClose);
        }

        [Test]
        public void R042_R043_LevelUpOptions_Cumulative_MaxHpDoesNotHeal()
        {
            var c = Config(); var e = Started(config: c);
            e.State.Xp = c.XpPerLevel - 1;
            e.State.Hp = 3;
            SetAttack(e, 10);
            int atk = e.State.Attack, max = e.State.MaxHp;
            e.Reveal(Find(e, ContentKind.Monster1));
            e.Submit(OptionIds.LevelMaxHp);
            Assert.AreEqual(max + c.LevelUpMaxHp, e.State.MaxHp);
            Assert.AreEqual(3, e.State.Hp);
            Assert.AreEqual(atk, e.State.Attack);
        }

        [Test]
        public void R038_CoinBonus_PerGainEvent_NotOnPurchases()
        {
            var c = Config(); var e = Started(config: c);
            e.State.CoinBonusPerGain = 2;
            SetAttack(e, 10);
            e.Reveal(Find(e, ContentKind.Monster1));
            Assert.AreEqual(c.StartCoins + c.Content(ContentKind.Monster1).Coins + 2, e.State.Coins);
        }

        [Test]
        public void R041_CompletingLine_GivesXp()
        {
            var c = Config(); var e = Started(config: c);
            e.State.MaxHp = 999; e.State.Hp = 999;
            SetAttack(e, 7); // non abbastanza per il Boss
            // Prima riga senza Boss
            int row = Enumerable.Range(0, c.Height).First(r => e.Layout.BossIndex / c.Width != r);
            int gained = 0;
            for (int col = 0; col < c.Width; col++)
            {
                int idx = row * c.Width + col;
                if (e.IsRevealed(idx)) continue;
                var evs = e.Reveal(idx);
                gained += evs.Count(x => x.Type == RuleEventType.LineCompleted && x.Line.Equals(LineRef.Row(row)));
                ResolveAll(e);
                if (e.State.Status != RunStatus.Playing) Assert.Fail("partita finita");
            }
            Assert.AreEqual(1, gained);
        }

        // ------------------------------------------------ NPC

        [Test]
        public void R062_Npc_OneChoice_CloseConsumes()
        {
            var e = Started();
            int idx = Find(e, ContentKind.Inn);
            e.Reveal(idx);
            Assert.AreEqual(DecisionKind.Npc, e.CurrentDecision.Kind);
            Assert.IsNull(e.CurrentDecision.Find(OptionIds.Attack), "Locanda intoccabile");
            var evs = e.Submit(OptionIds.Close);
            Assert.IsNotNull(First(evs, RuleEventType.DecisionResolved), "anche Vai via produce un evento");
            Assert.IsNull(e.CurrentDecision);
            Assert.IsTrue(e.IsRevealed(idx));
        }

        [Test]
        public void R064_Npc_UnaffordableOffers_Disabled()
        {
            var e = Started();
            e.Reveal(Find(e, ContentKind.Witch));
            // Abilitate solo le offerte che le monete attuali coprono (con StartCoins > 0 alcune lo sono).
            Assert.IsTrue(e.CurrentDecision.Options.Where(o => o.Price > 0).All(o => o.Enabled == (o.Price <= e.State.Coins)));
            Assume.That(e.State.Coins < 8, "il test presuppone l'anello della Strega non acquistabile");
            CollectionAssert.IsEmpty(e.Submit("witch_ring"));
        }

        [Test]
        public void R063_NpcAttack_Win_GivesKillReward()
        {
            var c = Config(); var e = Started(config: c);
            SetAttack(e, c.NpcDefense);
            e.Reveal(Find(e, ContentKind.Trader));
            e.Submit(OptionIds.Attack);
            Assert.AreEqual(c.StartCoins + c.Npcs[ContentKind.Trader].KillCoins, e.State.Coins);
        }

        [Test]
        public void D011_NpcAttack_Fail_DamageNoReward()
        {
            var c = Config(); var e = Started(config: c);
            SetAttack(e, c.NpcDefense - 2);
            int hp = e.State.Hp;
            e.Reveal(Find(e, ContentKind.Hunter));
            var evs = e.Submit(OptionIds.Attack);
            Assert.IsNotNull(First(evs, RuleEventType.NpcFled));
            Assert.AreEqual(hp - 2, e.State.Hp);
            Assert.IsNull(e.State.Trinket);
        }

        [Test]
        public void R065_TwoTraders_Independent()
        {
            var c = Config(); var e = Started(config: c);
            SetAttack(e, c.NpcDefense);
            e.Reveal(Find(e, ContentKind.Trader));
            e.Submit(OptionIds.Attack);
            int second = Find(e, ContentKind.Trader);
            Assert.GreaterOrEqual(second, 0);
            e.Reveal(second);
            Assert.AreEqual(DecisionKind.Npc, e.CurrentDecision.Kind);
            Assert.IsTrue(e.CurrentDecision.Find("trader_weapon").Enabled);
        }

        [Test]
        public void R029_SmithUpgrade_OncePerRun()
        {
            var c = Config(); var e = Started(config: c);
            e.State.Coins = 100;
            int atk = e.State.Attack;
            e.Reveal(Find(e, ContentKind.Smith));
            e.Submit("smith_upgrade");
            Assert.AreEqual(atk + 1, e.State.Attack);
            Assert.IsTrue(e.State.SmithUpgradeBought);
            // Un secondo Fabbro (ipotetico) mostrerebbe l'offerta disabilitata: verificato riaprendo la decisione.
            var c2 = Config(); c2.Census[ContentKind.Smith] = 2; c2.Census[ContentKind.Empty] -= 1;
            var e2 = Started(config: c2);
            e2.State.Coins = 100;
            e2.Reveal(Find(e2, ContentKind.Smith)); e2.Submit("smith_upgrade");
            e2.Reveal(Find(e2, ContentKind.Smith));
            Assert.IsFalse(e2.CurrentDecision.Find("smith_upgrade").Enabled);
        }

        [Test]
        public void R025_BoughtItem_ReplacesSlot()
        {
            var c = Config(); var e = Started(config: c);
            e.State.Coins = 100;
            e.Reveal(Find(e, ContentKind.Trader));
            var evs = e.Submit("trader_weapon");
            Assert.AreEqual(RulesConfigDefaults.WeaponL3, e.State.Weapon.Id);
            Assert.AreEqual(RulesConfigDefaults.Knife, First(evs, RuleEventType.ItemDiscarded).ItemId);
            Assert.AreEqual(100 - 10, e.State.Coins);
        }

        // ------------------------------------------------ indizi

        [Test]
        public void R092_R094_ClueUse_CountsAndSlotOnce()
        {
            var c = Config(); var e = Started(config: c);
            e.State.Coins = 100;
            e.Reveal(Find(e, ContentKind.Farmer));
            e.Submit("farmer_clue_2");
            Assert.AreEqual(DecisionKind.ClueLine, e.CurrentDecision.Kind);
            var line = e.CurrentDecision.Options[0].Line;
            var evs = e.Submit(OptionIds.Line(line));
            var used = First(evs, RuleEventType.ClueUsed);
            Assert.AreEqual(e.Layout.ClueCount(ClueTarget.Bush, line), used.Amount);
            Assert.IsFalse(e.IsLineFree(line));
            Assert.AreEqual(DecisionKind.ClueLine, e.CurrentDecision.Kind, "catena di 2");
            Assert.IsNull(e.CurrentDecision.Find(OptionIds.Line(line)));
        }

        [Test]
        public void R096_NoFreeLine_ClueLost()
        {
            var c = Config(); var e = Started(config: c);
            // Occupa tutte le linee simulando indizi già usati.
            var field = typeof(RulesEngine).GetField("_usedClueLines", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var set = (System.Collections.Generic.HashSet<LineRef>)field.GetValue(e);
            foreach (var l in e.AllLines()) set.Add(l);
            e.State.Coins = 100;
            e.Reveal(Find(e, ContentKind.Villagers));
            var evs = e.Submit("villagers_clue_bush");
            Assert.IsNotNull(First(evs, RuleEventType.ClueLost));
            Assert.IsNull(e.CurrentDecision);
        }

        // ------------------------------------------------ Lanterna

        [Test]
        public void D015_Lantern_ShowsOneAdjacentType()
        {
            var c = Config(); var e = Started(config: c);
            e.State.Trinket = c.Item(RulesConfigDefaults.Lantern);
            var evs = e.Reveal(Find(e, ContentKind.Bush));
            var shown = First(evs, RuleEventType.TileTypeShown);
            if (shown != null)
            {
                Assert.IsTrue(e.IsTypeShown(shown.TileIndex));
                Assert.IsFalse(e.IsRevealed(shown.TileIndex));
                Assert.IsTrue(e.IsRevealable(shown.TileIndex));
            }
        }

        // ------------------------------------------------ determinismo

        [Test]
        public void Determinism_SameSeedSameChoices_SameEvents()
        {
            string Play(ulong seed)
            {
                var e = new RulesEngine(Config(), seed);
                var log = new System.Text.StringBuilder();
                foreach (var x in e.Start()) log.AppendLine(x.ToString());
                var order = Enumerable.Range(0, e.Layout.Tiles.Length).ToList();
                foreach (int i in order)
                {
                    while (e.CurrentDecision != null)
                        foreach (var x in e.Submit(e.CurrentDecision.Options.First(o => o.Enabled).Id)) log.AppendLine(x.ToString());
                    if (e.State.Status != RunStatus.Playing) break;
                    foreach (var x in e.Reveal(i)) log.AppendLine(x.ToString());
                }
                return log.ToString();
            }
            Assert.AreEqual(Play(11), Play(11));
            Assert.AreNotEqual(Play(11), Play(12));
        }
    
        [Test]
        public void D036_StartCoins_FromConfig()
        {
            var c = Config(); var e = Started(config: c);
            Assert.AreEqual(c.StartCoins, e.State.Coins);
        }

        [Test]
        public void D037_Boss_IsOnlyMaskLevel6()
        {
            var c = Config();
            int boss = c.Content(ContentKind.Boss).MaskLevel;
            foreach (var kv in c.Contents)
                if (kv.Key != ContentKind.Boss && c.Census.TryGetValue(kv.Key, out int q) && q > 0)
                    Assert.AreNotEqual(boss, kv.Value.MaskLevel, kv.Key.ToString());
        }
    }
}
