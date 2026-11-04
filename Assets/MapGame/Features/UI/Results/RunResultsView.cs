using System.Text;
using hp55games.MapGame.Features.Gameplay.Run;
using hp55games.MapGame.Features.UI.Run;
using hp55games.MapGame.Rules;
using hp55games.Mobile.Core.Architecture;
using TMPro;
using UnityEngine;

namespace hp55games.MapGame.Features.UI.Results
{
    /// <summary>
    /// Riepilogo della partita sulla pagina risultati (UX 4): esito con la causa, missione, statistiche e seed.
    /// Legge RunResults dal ServiceRegistry quando la pagina si apre.
    /// </summary>
    public sealed class RunResultsView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _cause;
        [SerializeField] private TextMeshProUGUI _stats;
        [SerializeField] private TextMeshProUGUI _seed;

        private void Awake()
        {
            if (_cause == null || _stats == null || _seed == null)
            {
                Debug.LogError("[RunResultsView] _cause, _stats o _seed non assegnati.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (!enabled) return;
            if (!ServiceRegistry.TryResolve(out RunResults r) || r == null)
            {
                _cause.text = ""; _stats.text = ""; _seed.text = "";
                return;
            }

            _cause.text = RunText.T(r.Status == RunStatus.Won ? "run.results.cause_won"
                : r.LossReason == LossReason.BossFled ? "run.results.cause_boss_fled" : "run.results.cause_dead");

            var sb = new StringBuilder();
            void Line(string key, object value) => sb.AppendLine(RunText.T(key, value));
            sb.AppendLine(RunText.T("run.results.mode", RunText.T("run.mode." + r.Mode.ToString().ToLowerInvariant())));
            if (r.MissionId != null) sb.AppendLine(RunText.T("run.mission." + r.MissionId));
            Line("run.results.level", r.Level);
            Line("run.results.attack", r.Attack);
            Line("run.results.kills", r.Kills);
            Line("run.results.minibosses", r.MiniBossKills);
            Line("run.results.npcs", r.NpcsMet);
            Line("run.results.clues", r.CluesUsed);
            Line("run.results.tiles", r.TilesRevealed);
            Line("run.results.coins", r.CoinsEarned);
            Line("run.results.damage", r.DamageTaken);
            _stats.text = sb.ToString().TrimEnd();

            _seed.text = RunText.T(r.IsDaily ? "run.results.seed_daily" : "run.results.seed", r.Seed);
        }
    }
}
