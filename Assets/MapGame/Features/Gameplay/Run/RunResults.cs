using System.Collections.Generic;
using hp55games.MapGame.Rules;

namespace hp55games.MapGame.Features.Gameplay.Run
{
    /// <summary>
    /// Statistiche della partita in corso, lette dalla pagina risultati (UX 4). Oggetto semplice registrato
    /// nel ServiceRegistry da HexGridController a ogni nuova partita: sopravvive allo scarico della scena di gioco.
    /// Si aggiorna solo dagli eventi del motore (la presentazione non calcola regole).
    /// </summary>
    public sealed class RunResults
    {
        public int Seed { get; private set; }
        public bool IsDaily { get; private set; }
        public string MissionId { get; private set; }
        public RunStatus Status { get; private set; }
        public LossReason LossReason { get; private set; }
        public int Level { get; private set; }
        public int Attack { get; private set; }
        public int Kills { get; private set; }
        public int MiniBossKills { get; private set; }
        public int NpcsMet { get; private set; }
        public int CluesUsed { get; private set; }
        public int TilesRevealed { get; private set; }
        public int CoinsEarned { get; private set; }
        public int DamageTaken { get; private set; }

        public RunResults(int seed, bool isDaily)
        {
            Seed = seed;
            IsDaily = isDaily;
        }

        public void Apply(IReadOnlyList<RuleEvent> events, RulesEngine engine, bool initial)
        {
            foreach (var e in events)
            {
                switch (e.Type)
                {
                    case RuleEventType.TileRevealed: if (!initial) TilesRevealed++; break;
                    case RuleEventType.EncounterWon:
                        Kills++;
                        if (e.Kind == ContentKind.MiniBossWeapon || e.Kind == ContentKind.MiniBossSilverKey
                            || e.Kind == ContentKind.MiniBossGoldKey || e.Kind == ContentKind.MiniBossCoins) MiniBossKills++;
                        break;
                    case RuleEventType.NpcKilled: Kills++; break;
                    case RuleEventType.NpcOpened: NpcsMet++; break;
                    case RuleEventType.ClueUsed: CluesUsed++; break;
                    case RuleEventType.CoinsGained: CoinsEarned += e.Amount; break;
                    case RuleEventType.DamageTaken: DamageTaken += e.Amount; break;
                }
            }
            var s = engine.State;
            MissionId = s.Mission?.Id;
            Status = s.Status;
            LossReason = s.LossReason;
            Level = s.Level;
            Attack = s.Attack;
        }
    }

    /// <summary>
    /// Richiesta di seed per la prossima partita (mappa del giorno). Il contesto del template azzera
    /// CurrentRunSeed all'ingresso in Gameplay, quindi la richiesta passa da qui; HexGridController la consuma.
    /// </summary>
    public sealed class RunSeedRequest
    {
        public int Seed { get; private set; }
        public bool IsDaily { get; private set; }
        public bool HasRequest => Seed != 0;

        public void RequestDaily(int seed) { Seed = seed; IsDaily = true; }

        public void Clear() { Seed = 0; IsDaily = false; }

        /// <summary>Seed del giorno (UTC): stesso valore per tutti i giocatori nello stesso giorno, mai 0.</summary>
        public static int DailySeed(int year, int month, int day)
        {
            unchecked
            {
                uint h = (uint)(year * 10000 + month * 100 + day);
                h ^= h >> 16; h *= 0x7feb352d; h ^= h >> 15; h *= 0x846ca68b; h ^= h >> 16;
                int seed = (int)(h & 0x7fffffff);
                return seed == 0 ? 1 : seed;
            }
        }
    }
}
