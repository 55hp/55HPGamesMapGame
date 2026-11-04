using System.Collections.Generic;
using hp55games.MapGame.Features.Gameplay.Run;
using hp55games.MapGame.Rules;
using hp55games.Mobile.Core.Architecture;
using TMPro;
using UnityEngine;

namespace hp55games.MapGame.Features.UI.Run
{
    /// <summary>
    /// Banner breve per i momenti importanti della partita: nuovo livello, chiave ottenuta.
    /// Compare, resta qualche istante e svanisce; i banner dello stesso lotto si accodano.
    /// Si collega da sola a IRunSession, come FloatingTextView.
    /// </summary>
    public sealed class RunBannerView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private TextMeshProUGUI _text;

        [Header("Animazione")]
        [SerializeField] private float _fadeIn = 0.15f;
        [SerializeField] private float _hold = 1.2f;
        [SerializeField] private float _fadeOut = 0.35f;
        [SerializeField] private float _popScale = 1.15f;

        private readonly Queue<string> _queue = new Queue<string>();
        private IRunSession _session;
        private float _time = -1f;

        private float Total => _fadeIn + _hold + _fadeOut;

        private void Awake()
        {
            if (_group == null || _text == null)
            {
                Debug.LogError("[RunBannerView] _group o _text non assegnati.", this);
                enabled = false;
                return;
            }
            Hide();
        }

        private void Update()
        {
            if (_session == null || (_session is Object o && o == null)) TryBind();

            if (_time < 0f)
            {
                if (_queue.Count > 0) Show(_queue.Dequeue());
                return;
            }
            _time += Time.deltaTime;
            float t = _time;
            float a = t < _fadeIn ? t / _fadeIn : t < _fadeIn + _hold ? 1f : 1f - (t - _fadeIn - _hold) / _fadeOut;
            _group.alpha = Mathf.Clamp01(a);
            float s = t < _fadeIn ? Mathf.Lerp(_popScale, 1f, t / _fadeIn) : 1f;
            transform.localScale = new Vector3(s, s, 1f);
            if (_time >= Total) Hide();
        }

        private void OnDestroy() => Unbind();

        private void TryBind()
        {
            _session = null;
            if (!ServiceRegistry.TryResolve(out IRunSession s) || s == null || (s is Object o && o == null)) return;
            _session = s;
            _session.RuleEventsRaised += OnEvents;
            _session.SessionStarted += Clear;
        }

        private void Unbind()
        {
            if (_session == null) return;
            _session.RuleEventsRaised -= OnEvents;
            _session.SessionStarted -= Clear;
            _session = null;
        }

        private void OnEvents(IReadOnlyList<RuleEvent> events)
        {
            foreach (var ev in events)
            {
                switch (ev.Type)
                {
                    case RuleEventType.LevelUp: _queue.Enqueue(RunText.T("run.banner.level_up", ev.Amount)); break;
                    case RuleEventType.PhoenixSaved: _queue.Enqueue(RunText.T("run.banner.phoenix")); break;
                    case RuleEventType.MissionReached: _queue.Enqueue(RunText.T("run.banner.mission_reached", ev.Amount)); break;
                    case RuleEventType.KeyObtained:
                        _queue.Enqueue(RunText.T(ev.Key == KeyKind.Gold ? "run.banner.key_gold" : "run.banner.key_silver"));
                        break;
                }
            }
        }

        private void Show(string text)
        {
            _text.text = text;
            _time = 0f;
            _group.alpha = 0f;
        }

        private void Hide()
        {
            _time = -1f;
            _group.alpha = 0f;
            transform.localScale = Vector3.one;
        }

        private void Clear()
        {
            _queue.Clear();
            Hide();
        }
    }
}
