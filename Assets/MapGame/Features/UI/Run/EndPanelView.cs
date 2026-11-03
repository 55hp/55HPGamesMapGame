using hp55games.MapGame.Features.Gameplay.Run;
using hp55games.MapGame.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace hp55games.MapGame.Features.UI.Run
{
    /// <summary>Esito della partita (06 §4, fine partita). "Continua" passa alla schermata dei risultati.</summary>
    public sealed class EndPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private TextMeshProUGUI _summary;
        [SerializeField] private Button _continue;

        private IRunSession _session;

        private void Awake()
        {
            if (_root == null || _title == null || _summary == null || _continue == null)
            {
                Debug.LogError("[EndPanelView] Riferimenti mancanti nel prefab.", this);
                enabled = false;
                return;
            }
            _root.SetActive(false);
            _continue.onClick.AddListener(() =>
            {
                _session?.BlockBoardInput();
                _session?.ConfirmEnd();
            });
        }

        private void Update()
        {
            if (_root.activeSelf) _session?.BlockBoardInput();
        }

        public void Bind(IRunSession session) => _session = session;

        public void Refresh(RulesEngine e)
        {
            if (!enabled) return;
            var s = e.State;
            bool show = s.Status != RunStatus.Playing && _session != null && _session.IsAwaitingEndConfirm;
            _root.SetActive(show);
            if (!show) return;
            _title.text = RunText.T(s.Status == RunStatus.Won ? "run.end.won" : s.LossReason == LossReason.BossFled ? "run.end.boss_fled" : "run.end.dead");
            _summary.text = RunText.T("run.end.summary", s.Level, s.Attack, s.Coins);
        }
    }
}
