using System.Collections.Generic;
using hp55games.MapGame.Features.Gameplay.Run;
using hp55games.MapGame.Rules;
using UnityEngine;

namespace hp55games.MapGame.Features.UI.Run
{
    /// <summary>
    /// Intestazioni 1–10 (righe, a sinistra) e A–J (colonne, sotto) agganciate alla griglia (05 §5).
    /// Mostrano il risultato degli indizi e, mentre se ne posa uno, diventano i pulsanti per scegliere la linea.
    /// </summary>
    public sealed class BoardLineLabelsView : MonoBehaviour
    {
        [Tooltip("RectTransform a tutto schermo dentro cui si posizionano le etichette.")]
        [SerializeField] private RectTransform _container;
        [SerializeField] private LineLabelView _labelPrefab;
        [Tooltip("Distanza dal bordo della griglia, in celle.")]
        [SerializeField] private float _offsetCells = 1.0f;

        private readonly Dictionary<LineRef, LineLabelView> _labels = new Dictionary<LineRef, LineLabelView>();
        private IRunSession _session;
        private Canvas _canvas;

        private void Awake()
        {
            if (_container == null || _labelPrefab == null)
            {
                Debug.LogError("[BoardLineLabelsView] _container o _labelPrefab non assegnati.", this);
                enabled = false;
                return;
            }
            _canvas = _container.GetComponentInParent<Canvas>();
        }

        public void Bind(IRunSession session) => _session = session;

        public void Refresh(RulesEngine e)
        {
            if (!enabled) return;
            var d = e.CurrentDecision;
            // Etichette cliccabili per posare un indizio o una Ricognizione di riga/colonna (D-045).
            bool choosing = d != null && (d.Kind == DecisionKind.ClueLine || (d.Kind == DecisionKind.Scout && ScoutShapes.IsLine(d.ScoutShape)));
            foreach (var line in e.AllLines())
            {
                var view = Get(line);
                string clue = e.ClueResults.TryGetValue(line, out var res) ? RunText.ClueShort(res.target) + " " + res.count : "";
                var opt = choosing ? d.Find(OptionIds.Line(line)) : null;
                string id = opt?.Id;
                view.Set(line.Label(e.Config.Height), clue, opt != null, () =>
                {
                    _session?.BlockBoardInput();
                    if (id != null) _session?.Submit(id);
                });
            }
        }

        private void LateUpdate()
        {
            var e = _session?.Engine;
            if (e == null || _labels.Count == 0) return;
            int w = e.Config.Width, h = e.Config.Height;
            if (!_session.TryGetScreenPoint(0, 0, out var c00) || !_session.TryGetScreenPoint(1, 0, out var c10)
                || !_session.TryGetScreenPoint(0, 1, out var c01)) return;
            float stepX = c10.x - c00.x;
            float stepY = c01.y - c00.y;

            foreach (var kv in _labels)
            {
                var line = kv.Key;
                Vector2 screen;
                if (line.IsRow)
                {
                    _session.TryGetScreenPoint(0, line.Index, out screen);
                    screen.x -= stepX * _offsetCells;
                }
                else
                {
                    _session.TryGetScreenPoint(line.Index, 0, out screen);
                    screen.y = c00.y - stepY * _offsetCells;
                }
                var cam = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_container, screen, cam, out var local))
                    kv.Value.Rect.anchoredPosition = local;
            }
        }

        private LineLabelView Get(LineRef line)
        {
            if (_labels.TryGetValue(line, out var v)) return v;
            v = Instantiate(_labelPrefab, _container);
            var rt = v.Rect;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            _labels[line] = v;
            return v;
        }
    }
}
