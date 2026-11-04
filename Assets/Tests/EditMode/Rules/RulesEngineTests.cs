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
            Assert.AreEqual(c.StartWeaponId, e.State.MainHand.Id);
            Assert.AreEqual(c.BaseAttack + c.Item(c.StartWeaponId).AttackBonus, e.State.Attack);
            Assert.AreEqual(RunStatus.Playing, e.State.Status);
        }

        [Test]
        public void D041_OnlyStartRevealed_EmptiesCovered_BossHidden()
        {
            var e = Started();
            for (int i = 0; i < e.Layout.Tiles.Length; i++)
            {
                var k = e.Layout.Tiles[i];
                Assert.AreEqual(k == ContentKind.Start, e.IsRevealed(i), $"{k}");
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

            e.State.Give(c.Item(RulesConfigDefaults.HunterTools));
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
            Assert.AreEqual(Config().StartWeaponId, e.State.MainHand.Id);
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
            e.State.OffHand = c.Item(RulesConfigDefaults.WeaponL2); // D-050: mani piene, serve la decisione
            e.Reveal(Find(e, ContentKind.ChestSilver));
            Assert.AreEqual(DecisionKind.Item, e.CurrentDecision.Kind);
            Assert.AreEqual(RulesConfigDefaults.WeaponL4, e.CurrentDecision.NewItemId);
            e.Submit(OptionIds.EquipReplacing(RulesConfigDefaults.Knife));
            Assert.IsTrue(e.State.HasItem(RulesConfigDefaults.WeaponL4));
            Assert.IsFalse(e.State.HasItem(RulesConfigDefaults.Knife));
        }

        // ------------------------------------------------ equipaggiamento

        [Test]
        public void R025_FoundItem_Discard_KeepsOld()
        {
            var c = Config(); var e = Started(config: c);
            e.State.OffHand = c.Item(RulesConfigDefaults.WeaponL3); // D-050: mani piene
            e.Reveal(Find(e, ContentKind.ChestSimpleWeapon));
            Assert.AreEqual(DecisionKind.Item, e.CurrentDecision.Kind);
            var evs = e.Submit(OptionIds.Discard);
            Assert.AreEqual(RulesConfigDefaults.Knife, e.State.MainHand.Id);
            Assert.AreEqual(RulesConfigDefaults.WeaponL3, e.State.OffHand.Id);
            Assert.AreEqual(RulesConfigDefaults.WeaponL2, First(evs, RuleEventType.ItemDiscarded).ItemId);
        }

        [Test]
        public void R023_AttackSumsAllBonuses()
        {
            var c = Config(); var e = Started(config: c);
            e.State.MainHand = c.Item(RulesConfigDefaults.WeaponL3);
            e.State.OffHand = c.Item(RulesConfigDefaults.Ring2);
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
            Assert.AreEqual(0, e.State.Trinkets.Count);
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
            Assert.IsTrue(e.State.HasItem(RulesConfigDefaults.Whetstone)); // D-051: Trinket
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
            e.State.OffHand = c.Item(RulesConfigDefaults.WeaponL2); // D-050: mani piene
            e.Submit("trader_weapon");
            var evs = e.Submit(OptionIds.EquipReplacing(RulesConfigDefaults.Knife));
            Assert.IsTrue(e.State.HasItem(RulesConfigDefaults.WeaponL3));
            Assert.AreEqual(RulesConfigDefaults.Knife, First(evs, RuleEventType.ItemDiscarded).ItemId);
            Assert.AreEqual(100 - c.Npcs[ContentKind.Trader].Offers.First(o => o.Id == "trader_weapon").Price, e.State.Coins);
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
            e.State.Give(c.Item(RulesConfigDefaults.Lantern));
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
    
        [Test]
        public void D038_TraderClue_TargetIsCoveredLevel5()
        {
            var c = Config(); var e = Started(config: c);
            e.Reveal(Find(e, ContentKind.Trader));
            var clue = e.CurrentDecision.Options.FirstOrDefault(o => o.Id == "trader_clue_1");
            Assert.IsNotNull(clue);
            bool level5 = c.Contents.Values.Any(d => d.ClueTarget == clue.ClueTarget && d.MaskLevel == 5);
            Assert.IsTrue(level5, clue.ClueTarget.ToString());
            Assert.IsTrue(e.HasCoveredTarget(clue.ClueTarget));
        }

        [Test]
        public void D038_NoClueOffer_WhenTargetAllRevealed()
        {
            var c = Config(); var e = Started(config: c);
            // Scopre tutti gli arbusti, poi apre un Contadino: le sue offerte di indizi sugli Arbusti non compaiono.
            e.State.Hp = 100; e.State.MaxHp = 100;
            for (int i = 0; i < e.Layout.Tiles.Length; i++)
                if (e.Layout.Tiles[i] == ContentKind.Bush && !e.IsRevealed(i)) { e.Reveal(i); ResolveAll(e); }
            Assert.IsFalse(e.HasCoveredTarget(ClueTarget.Bush));
            e.Reveal(Find(e, ContentKind.Farmer));
            Assert.IsFalse(e.CurrentDecision.Options.Any(o => o.Id.StartsWith("farmer_clue")));
        }

        [Test]
        public void D039_Monster2Kill_GivesBushClue()
        {
            var c = Config(); var e = Started(config: c);
            SetAttack(e, c.Content(ContentKind.Monster2).Defense);
            var evs = e.Reveal(Find(e, ContentKind.Monster2));
            Assert.IsTrue(evs.Any(x => x.Type == RuleEventType.ClueReceived && x.ClueTarget == c.MonsterKillClueTarget));
        }
    
        [Test]
        public void D041_EmptyTile_RevealsWithoutEffect()
        {
            var e = Started();
            int idx = Find(e, ContentKind.Empty);
            var hp = e.State.Hp; var coins = e.State.Coins;
            var evs = e.Reveal(idx);
            Assert.IsTrue(e.IsRevealed(idx));
            Assert.AreEqual(hp, e.State.Hp); Assert.AreEqual(coins, e.State.Coins);
            Assert.IsTrue(evs.Any(x => x.Type == RuleEventType.TileRevealed));
        }

        [Test]
        public void D043_Fog_NoBordersExceptScouted_ClickFree()
        {
            var c = Config(); c.Mode = GameMode.Fog; c.FogStartScouts.Clear();
            var e = Started(config: c);
            for (int i = 0; i < e.Layout.Tiles.Length; i++)
                if (!e.IsRevealed(i)) { Assert.IsFalse(e.IsMaskVisible(i), $"tile {i}"); Assert.IsTrue(e.IsRevealable(i)); }
        }

        [Test]
        public void D043_FogShowBoss_BossBorderVisible()
        {
            var c = Config(); c.Mode = GameMode.Fog; c.FogShowBoss = true; c.FogStartScouts.Clear();
            var e = Started(config: c);
            Assert.IsTrue(e.IsMaskVisible(e.Layout.BossIndex));
        }

        [Test]
        public void D043_Scout_Radius1_ShowsCenterAndNeighbors()
        {
            var c = Config(); c.Mode = GameMode.Fog; c.FogStartScouts = new System.Collections.Generic.List<int> { 1 };
            var e = new RulesEngine(c, 1);
            e.Start();
            while (e.CurrentDecision != null && e.CurrentDecision.Kind != DecisionKind.Scout)
                e.Submit(e.CurrentDecision.Options.First(o => o.Enabled).Id);
            Assert.IsNotNull(e.CurrentDecision);
            int center = 55;
            var evs = e.Submit(OptionIds.Tile(center));
            Assert.IsTrue(evs.Any(x => x.Type == RuleEventType.ScoutUsed));
            if (!e.IsRevealed(center)) Assert.IsTrue(e.IsMaskVisible(center));
            foreach (int nb in HexGridMath.Neighbors(center, c.Width, c.Height))
                if (!e.IsRevealed(nb)) Assert.IsTrue(e.IsMaskVisible(nb));
        }

        [Test]
        public void D043_ScoutOffers_AbsentInOpenMode()
        {
            var c = Config(); var e = Started(config: c);
            e.Reveal(Find(e, ContentKind.Inn));
            Assert.IsFalse(e.CurrentDecision.Options.Any(o => o.Id.Contains("scout")));
        }

        [Test]
        public void D040_FogAdjacent_OnlyAdjacentRevealable()
        {
            var c = Config(); c.Mode = GameMode.FogAdjacent; var e = Started(config: c);
            for (int i = 0; i < e.Layout.Tiles.Length; i++)
                if (!e.IsRevealed(i)) Assert.AreEqual(e.IsAdjacentToRevealed(i), e.IsRevealable(i), $"tile {i}");
        }
    
        [Test]
        public void D045_RowScout_ByTileOrLabel_ShowsWholeRow()
        {
            var c = Config(); c.Mode = GameMode.Fog; c.FogStartScouts = new System.Collections.Generic.List<int> { ScoutShapes.Row, ScoutShapes.Column };
            var e = new RulesEngine(c, 3);
            e.Start();
            while (e.CurrentDecision != null && e.CurrentDecision.Kind != DecisionKind.Scout)
                e.Submit(e.CurrentDecision.Options.First(o => o.Enabled).Id);
            Assert.AreEqual(ScoutShapes.Row, e.CurrentDecision.ScoutShape);
            int w = c.Width;
            int tile = 4 * w + 7; // riga 4 (indice motore), colonna 7
            e.Submit(OptionIds.Tile(tile));
            for (int col = 0; col < w; col++) { int i = 4 * w + col; if (!e.IsRevealed(i)) Assert.IsTrue(e.IsMaskVisible(i), $"riga, tile {i}"); }
            Assert.AreEqual(ScoutShapes.Column, e.CurrentDecision.ScoutShape);
            e.Submit(OptionIds.Line(LineRef.Col(2)));
            for (int r = 0; r < c.Height; r++) { int i = r * w + 2; if (!e.IsRevealed(i)) Assert.IsTrue(e.IsMaskVisible(i), $"colonna, tile {i}"); }
        }

        [Test]
        public void D045_LineScoutOffers_Priced()
        {
            var c = Config(); c.Mode = GameMode.Fog; var e = Started(config: c);
            e.State.Coins = 100;
            e.Reveal(Find(e, ContentKind.Inn));
            var row = e.CurrentDecision.Options.First(o => o.Id == "inn_scout_row");
            Assert.AreEqual(c.ScoutPriceLine, row.Price);
        }
    
        [Test]
        public void D049_Compass_OfferedOnlyInFog()
        {
            foreach (var mode in new[] { GameMode.Open, GameMode.Fog }) // in Esplorazione il Mercante non è raggiungibile al primo tocco
            {
                var c = Config(); c.Mode = mode; var e = Started(config: c);
                e.State.Coins = 100;
                e.Reveal(Find(e, ContentKind.Trader));
                Assert.AreEqual(mode == GameMode.Fog, e.CurrentDecision.Options.Any(o => o.Id == "trader_compass"), mode.ToString());
            }
        }

        [Test]
        public void D049_Compass_ShowsAdjacentBordersInFog()
        {
            var c = Config(); c.Mode = GameMode.Fog; c.FogStartScouts = new System.Collections.Generic.List<int>(); var e = Started(config: c);
            int start = e.Layout.StartIndex;
            var nbs = HexGridMath.Neighbors(start, c.Width, c.Height).Where(n => !e.IsRevealed(n)).ToList();
            Assert.IsTrue(nbs.Any(n => !e.IsMaskVisible(n)));
            e.State.Give(c.Item(RulesConfigDefaults.Compass));
            foreach (int n in nbs) Assert.IsTrue(e.IsMaskVisible(n), $"tile {n}");
        }

        [Test]
        public void D049_Purse_AddsCoinsPerGain()
        {
            var c = Config(); var e = Started(config: c);
            e.State.Give(c.Item(RulesConfigDefaults.Purse));
            int before = e.State.Coins;
            e.Reveal(Find(e, ContentKind.ChestSimpleCoins));
            ResolveAll(e);
            Assert.AreEqual(before + c.Content(ContentKind.ChestSimpleCoins).Coins + c.PurseBonus, e.State.Coins);
        }

        [Test]
        public void D049_TreasureMap_ShowsGoldChest()
        {
            var c = Config(); var e = Started(config: c);
            e.State.Coins = 100;
            int gold = Find(e, ContentKind.ChestGold);
            e.Reveal(Find(e, ContentKind.Trader));
            Assert.AreEqual(c.TreasureMapPrice, e.CurrentDecision.Find("trader_map_gold").Price);
            e.Submit("trader_map_gold");
            Assert.IsTrue(e.IsTypeShown(gold));
        }

        [Test]
        public void D050_TwoOneHandedWeapons_BestPlusOffHandBonus()
        {
            var c = Config(); var e = Started(config: c);
            e.State.MainHand = c.Item(RulesConfigDefaults.WeaponL3);
            int before = e.State.Attack;
            e.Reveal(Find(e, ContentKind.ChestSimpleWeapon)); // mano 2 libera: si equipaggia da sola
            Assert.IsNull(e.CurrentDecision);
            Assert.AreEqual(RulesConfigDefaults.WeaponL2, e.State.OffHand.Id);
            Assert.AreEqual(before + c.OffHandWeaponBonus, e.State.Attack);
        }

        [Test]
        public void D050_KnifeInOffHand_DoesNotCount()
        {
            var c = Config(); var e = Started(config: c);
            Assert.Less(c.Item(RulesConfigDefaults.Knife).AttackBonus, c.OffHandWeaponMinBonus);
            e.State.MainHand = c.Item(RulesConfigDefaults.WeaponL3);
            e.State.OffHand = c.Item(RulesConfigDefaults.Knife);
            Assert.AreEqual(c.BaseAttack + c.Item(RulesConfigDefaults.WeaponL3).AttackBonus, e.State.Attack);
        }

        [Test]
        public void D050_TwoHandedWeapon_ReplacesBothHands()
        {
            var c = Config(); var e = Started(config: c);
            e.State.OffHand = c.Item(RulesConfigDefaults.Ring2);
            e.State.Coins = 100;
            e.State.LevelAttackBonus = 10;
            e.Reveal(Find(e, ContentKind.Hunter));
            e.Submit(OptionIds.Attack); // l'Arco a due mani
            var d = e.CurrentDecision;
            Assert.AreEqual(DecisionKind.Item, d.Kind);
            Assert.IsNotNull(d.Find(OptionIds.EquipAll));
            e.Submit(OptionIds.EquipAll);
            Assert.AreEqual(RulesConfigDefaults.HunterBow, e.State.MainHand.Id);
            Assert.IsNull(e.State.OffHand);
            Assert.AreEqual(0, e.State.FreeHands);
        }

        [Test]
        public void D050_Trinkets_Stack_OwnedOfferDisabled()
        {
            var c = Config(); var e = Started(config: c);
            e.State.Give(c.Item(RulesConfigDefaults.Lantern));
            e.State.Give(c.Item(RulesConfigDefaults.HunterTools));
            Assert.IsTrue(e.State.HasEffect(ItemEffect.Lantern));
            Assert.IsTrue(e.State.HasEffect(ItemEffect.DisarmTraps));
            e.State.Coins = 100;
            e.Reveal(Find(e, ContentKind.Witch));
            var lantern = e.CurrentDecision.Find("witch_lantern");
            Assert.IsFalse(lantern.Enabled);
            Assert.AreEqual("ui.npc.reason.already_owned", lantern.DisabledReasonKey);
        }

        [Test]
        public void D051_SilverMap_IsTrinket_ShowsSilverChest()
        {
            var c = Config(); var e = Started(config: c);
            e.State.Coins = 100;
            int silver = Find(e, ContentKind.ChestSilver);
            e.Reveal(Find(e, ContentKind.Trader));
            e.Submit("trader_map_silver");
            Assert.IsTrue(e.IsTypeShown(silver));
            Assert.IsTrue(e.State.HasItem(RulesConfigDefaults.MapSilver));
            Assert.IsTrue(c.Item(RulesConfigDefaults.MiniBossWeapon).TwoHanded);
        }

        [Test]
        public void D052_Shield_ReducesTrapDamage()
        {
            var c = Config(); var e = Started(config: c);
            e.State.Give(c.Item(RulesConfigDefaults.Shield));
            int hp = e.State.Hp;
            e.Reveal(Find(e, ContentKind.Trap2));
            Assert.AreEqual(hp - (c.Content(ContentKind.Trap2).TrapDamage - c.ShieldReduction), e.State.Hp);
        }

        [Test]
        public void D052_Phoenix_SavesOnce()
        {
            var c = Config(); var e = Started(config: c);
            e.State.Give(c.Item(RulesConfigDefaults.Phoenix));
            e.State.Hp = 1;
            var evs = e.Reveal(Find(e, ContentKind.Trap2));
            Assert.AreEqual(RunStatus.Playing, e.State.Status);
            Assert.AreEqual(c.PhoenixHp, e.State.Hp);
            Assert.IsNotNull(First(evs, RuleEventType.PhoenixSaved));
            Assert.IsFalse(e.State.HasItem(RulesConfigDefaults.Phoenix));
        }

        [Test]
        public void D052_Herbarium_BushHealsMore()
        {
            var c = Config(); var e = Started(config: c);
            e.State.Give(c.Item(RulesConfigDefaults.Herbarium));
            e.State.MaxHp = 20; e.State.Hp = 1;
            e.Reveal(Find(e, ContentKind.Bush));
            Assert.AreEqual(1 + c.Content(ContentKind.Bush).Heal + c.HerbariumBonus, e.State.Hp);
        }

        [Test]
        public void D053_Fog_NoClues_ScoutsStay()
        {
            var c = Config(); c.Mode = GameMode.Fog;
            var e = new RulesEngine(c, 1);
            var evs = e.Start();
            Assert.IsFalse(e.CluesActive);
            Assert.IsNull(First(evs, RuleEventType.ClueReceived));
            Assert.IsNotNull(First(evs, RuleEventType.ScoutReceived));
            ResolveAll(e);
            e.State.Coins = 100;
            e.Reveal(Find(e, ContentKind.Inn));
            Assert.IsFalse(e.CurrentDecision.Options.Any(o => o.Id.Contains("clue")));
            Assert.IsTrue(e.CurrentDecision.Options.Any(o => o.Id.Contains("scout")));
        }

        [Test]
        public void D054_Scouts_BuyWhileCoinsLast()
        {
            var c = Config(); c.Mode = GameMode.Fog; var e = Started(config: c);
            e.State.Coins = c.ScoutPriceSingle * 3;
            int npc = Find(e, ContentKind.Villagers);
            e.Reveal(npc);
            for (int k = 0; k < 3; k++)
            {
                Assert.AreEqual(DecisionKind.Npc, e.CurrentDecision.Kind, $"acquisto {k}");
                Assert.AreEqual(-1, e.CurrentDecision.Find("villagers_scout_0").Stock);
                e.Submit("villagers_scout_0");
                Assert.AreEqual(DecisionKind.Scout, e.CurrentDecision.Kind);
                e.Submit(e.CurrentDecision.Options.First(o => o.Enabled).Id);
            }
            Assert.AreEqual(0, e.State.Coins);
            Assert.AreEqual(DecisionKind.Npc, e.CurrentDecision.Kind);
            Assert.IsFalse(e.CurrentDecision.Find("villagers_scout_0").Enabled);
            e.Submit(OptionIds.Close);
            Assert.IsNull(e.CurrentDecision);
        }
}
}
