using System.Collections.Generic;
using hp55games.MapGame.Features.Gameplay.Run;
using hp55games.MapGame.Rules;
using hp55games.Mobile.Core.Architecture;
using UnityEngine;

namespace hp55games.MapGame.Features.UI.Run
{
    /// <summary>
    /// Numeri fluttuanti sopra la casella appena risolta: danno, cura, monete, XP.
    /// La casella di riferimento è l'ultimo evento del lotto con TileIndex valido (rivelazione, scontro, NPC).
    /// Si collega da sola a IRunSession (come RunUiRoot), quindi non serve altro wiring oltre ai campi qui sotto.
    /// </summary>
    public sealed class FloatingTextView : MonoBehaviour
    {
        [SerializeField] private RectTransform _container;
        [SerializeField] private FloatingTextItem _itemPrefab;

        [Header("Animazione")]
        [SerializeField] private float _duration = 1.1f;
        [SerializeField] private float _rise = 140f;
        [SerializeField] private float _stackSpacing = 56f;

        [Header("Colori")]
        [SerializeField] private Color _damageColor = new Color(1f, 0.35f, 0.35f);
        [SerializeField] private Color _healColor = new Color(0.45f, 1f, 0.5f);
        [SerializeField] private Color _coinsColor = new Color(1f, 0.84f, 0.3f);
        [SerializeField] private Color _xpColor = new Color(0.4f, 0.8f, 1f);

        private sealed class Live
        {
            public FloatingTextItem Item;
            public Vector2 Start;
            public float Time;
        }

        private readonly List<Live> _live = new List<Live>();
        private readonly Stack<FloatingTextItem> _pool = new Stack<FloatingTextItem>();
        private IRunSession _session;
        private Canvas _canvas;

        private void Awake()
        {
            if (_container == null || _itemPrefab == null || !_itemPrefab.IsValid)
            {
                Debug.LogError("[FloatingTextView] _container o _itemPrefab (con _text e _group) non assegnati.", this);
                enabled = false;
                return;
            }
            _canvas = _container.GetComponentInParent<Canvas>();
        }

        private void Update()
        {
            if (_session == null || (_session is Object o && o == null)) TryBind();
            Animate();
        }

        private void OnDestroy() => Unbind();

        private void TryBind()
        {
            _session = null;
            if (!ServiceRegistry.TryResolve(out IRunSession s) || s == null || (s is Object o && o == null)) return;
            _session = s;
            _session.RuleEventsRaised += OnEvents;
            _session.SessionStarted += ClearAll;
        }

        private void Unbind()
        {
            if (_session == null) return;
            _session.RuleEventsRaised -= OnEvents;
            _session.SessionStarted -= ClearAll;
            _session = null;
        }

        private void OnEvents(IReadOnlyList<RuleEvent> events)
        {
            var e = _session?.Engine;
            if (e == null) return;
            int anchor = -1, stack = 0;
            foreach (var ev in events)
            {
                if (ev.TileIndex >= 0) { anchor = ev.TileIndex; stack = 0; }
                string text; Color color;
                switch (ev.Type)
                {
                    case RuleEventType.DamageTaken: text = RunText.T("run.float.damage", ev.Amount); color = _damageColor; break;
                    case RuleEventType.Healed: text = RunText.T("run.float.heal", ev.Amount); color = _healColor; break;
                    case RuleEventType.CoinsGained: text = RunText.T("run.float.coins", ev.Amount); color = _coinsColor; break;
                    case RuleEventType.XpGained: text = RunText.T("run.float.xp", ev.Amount); color = _xpColor; break;
                    default: continue;
                }
                if (anchor < 0 || ev.Amount <= 0) continue;
                int w = e.Config.Width;
                if (!_session.TryGetScreenPoint(anchor % w, anchor / w, out var screen)) continue;
                var cam = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_container, screen, cam, out var local)) continue;
                Spawn(text, color, local + new Vector2(0f, stack * _stackSpacing));
                stack++;
            }
        }

        private void Spawn(string text, Color color, Vector2 at)
        {
            var item = _pool.Count > 0 ? _pool.Pop() : Instantiate(_itemPrefab, _container);
            item.gameObject.SetActive(true);
            item.Text.text = text;
            item.Text.color = color;
            item.Group.alpha = 1f;
            var rt = item.Rect;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = at;
            _live.Add(new Live { Item = item, Start = at, Time = 0f });
        }

        private void Animate()
        {
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                var l = _live[i];
                l.Time += Time.deltaTime; // in pausa timeScale = 0: i numeri si fermano
                float t = Mathf.Clamp01(l.Time / _duration);
                l.Item.Rect.anchoredPosition = l.Start + new Vector2(0f, _rise * (1f - (1f - t) * (1f - t)));
                l.Item.Group.alpha = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
                if (t >= 1f) Recycle(i);
            }
        }

        private void Recycle(int i)
        {
            var item = _live[i].Item;
            _live.RemoveAt(i);
            item.gameObject.SetActive(false);
            _pool.Push(item);
        }

        private void ClearAll()
        {
            for (int i = _live.Count - 1; i >= 0; i--) Recycle(i);
        }
    }
}
