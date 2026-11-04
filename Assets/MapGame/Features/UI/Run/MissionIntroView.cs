using System.Collections.Generic;
using hp55games.MapGame.Features.Gameplay.Run;
using hp55games.MapGame.Rules;
using hp55games.Mobile.Core.Architecture;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace hp55games.MapGame.Features.UI.Run
{
    /// <summary>
    /// D-046: all'avvio della partita mostra la missione estratta con un effetto roulette
    /// (i nomi delle missioni scorrono, rallentano e si fermano su quella pescata), poi la ricompensa.
    /// Finché è aperta copre la UI e blocca i tocchi sul tabellone. Si chiude con il pulsante o toccando ovunque.
    /// Si collega da sola a IRunSession, come RunBannerView.
    /// </summary>
    public sealed class MissionIntroView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private TextMeshProUGUI _title;      // "MISSIONE"
        [SerializeField] private TextMeshProUGUI _mission;    // nome che scorre
        [SerializeField] private TextMeshProUGUI _reward;     // ricompensa, compare a roulette ferma
        [SerializeField] private Button _closeButton;         // copre tutto lo schermo o è un pulsante "Via!"

        [Header("Roulette")]
        [SerializeField] private float _spinDuration = 1.6f;
        [SerializeField] private float _startInterval = 0.05f;
        [SerializeField] private float _endInterval = 0.28f;
        [SerializeField] private float _landPopScale = 1.25f;
        [SerializeField] private float _landPopTime = 0.2f;

        private readonly List<MissionDef> _missions = new List<MissionDef>();
        private IRunSession _session;
        private MissionDef _drawn;
        private RulesEngine _openedFor;
        private float _time = -1f;
        private float _nextSwap;
        private int _shown;
        private bool _landed;

        private void Awake()
        {
            if (_group == null || _mission == null)
            {
                Debug.LogError("[MissionIntroView] _group o _mission non assegnati.", this);
                enabled = false;
                return;
            }
            if (_closeButton != null) _closeButton.onClick.AddListener(Close);
            Hide();
        }

        private void Update()
        {
            if (_session == null || (_session is Object o && o == null)) TryBind();
            if (_time < 0f) return;

            _session?.BlockBoardInput();
            _time += Time.unscaledDeltaTime;

            if (!_landed)
            {
                float k = Mathf.Clamp01(_time / _spinDuration);
                if (_time >= _nextSwap)
                {
                    _shown = (_shown + 1) % _missions.Count;
                    if (k >= 1f) { Land(); return; }
                    if (_missions[_shown] == _drawn && k < 0.85f) _shown = (_shown + 1) % _missions.Count;
                    _mission.text = Name(_missions[_shown]);
                    _nextSwap = _time + Mathf.Lerp(_startInterval, _endInterval, k * k);
                }
                return;
            }

            float p = Mathf.Clamp01((_time - _spinDuration) / _landPopTime);
            float s = Mathf.Lerp(_landPopScale, 1f, p);
            _mission.transform.localScale = new Vector3(s, s, 1f);
            if (_reward != null) _reward.alpha = p;
        }

        private void OnDestroy()
        {
            if (_closeButton != null) _closeButton.onClick.RemoveListener(Close);
            Unbind();
        }

        private void TryBind()
        {
            _session = null;
            if (!ServiceRegistry.TryResolve(out IRunSession s) || s == null || (s is Object o && o == null)) return;
            _session = s;
            _session.RuleEventsRaised += OnEvents;
            _session.SessionStarted += Hide;
            // La partita può essere già partita prima che la view si colleghi: si legge lo stato.
            var st = _session.Engine?.State;
            if (st != null && st.Mission != null && st.Xp == 0 && st.Level == 1 && !st.MissionReached) Open(st.Mission);
        }

        private void Unbind()
        {
            if (_session == null) return;
            _session.RuleEventsRaised -= OnEvents;
            _session.SessionStarted -= Hide;
            _session = null;
        }

        private void OnEvents(IReadOnlyList<RuleEvent> events)
        {
            foreach (var ev in events)
                if (ev.Type == RuleEventType.MissionStarted && _session?.Engine?.State.Mission != null)
                    Open(_session.Engine.State.Mission);
        }

        private void Open(MissionDef drawn)
        {
            if (_session.Engine == _openedFor) return; // una volta per partita
            _openedFor = _session.Engine;
            _drawn = drawn;
            _missions.Clear();
            _missions.AddRange(_session.Engine.Config.Missions);
            if (_missions.Count == 0) _missions.Add(drawn);
            _shown = Random.Range(0, _missions.Count);
            _landed = _missions.Count <= 1;
            _time = _landed ? _spinDuration : 0f;
            _nextSwap = 0f;

            if (_title != null) _title.text = RunText.T("run.mission_intro.title");
            _mission.text = Name(_landed ? drawn : _missions[_shown]);
            _mission.transform.localScale = Vector3.one;
            if (_reward != null)
            {
                // D-053: in Nebbia la missione non dà indizi di partenza.
                _reward.text = (_session.Engine.CluesActive
                    ? RunText.T("run.mission_intro.reward", RunText.Clue(drawn.Target), drawn.StartClues)
                    : RunText.T("run.mission_intro.reward_noclues")).Replace("\\n", "\n");
                _reward.alpha = 0f;
            }
            _group.alpha = 1f;
            _group.blocksRaycasts = true;
            _group.interactable = true;
            transform.SetAsLastSibling();
        }

        private void Land()
        {
            _landed = true;
            _time = _spinDuration;
            _mission.text = Name(_drawn);
            if (ServiceRegistry.TryResolve(out hp55games.Mobile.Core.Haptics.IHapticsService h))
                h.Play(hp55games.Mobile.Core.Haptics.HapticStrength.Medium);
        }

        /// <summary>Il primo tocco ferma subito la roulette; il secondo chiude.</summary>
        private void Close()
        {
            if (_time < 0f) return;
            if (!_landed) { Land(); return; }
            Hide();
            _session?.BlockBoardInput();
        }

        private void Hide()
        {
            _time = -1f;
            _landed = false;
            if (_group == null) return;
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;
        }

        private static string Name(MissionDef m) => RunText.T("run.mission." + m.Id);
    }
}
