using System.Collections.Generic;
using System.Text;
using hp55games.MapGame.Features.Gameplay.Run;
using hp55games.MapGame.Rules;
using TMPro;
using UnityEngine;

namespace hp55games.MapGame.Features.UI.Run
{
    /// <summary>Registro degli ultimi eventi (feedback testuale di danno, cure, ricompense, indizi).</summary>
    public sealed class RunLogView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private int _maxLines = 4;

        private readonly List<string> _lines = new List<string>();

        private void Awake()
        {
            if (_text == null)
            {
                Debug.LogError("[RunLogView] _text non assegnato.", this);
                enabled = false;
            }
        }

        public void Bind(IRunSession session) { }

        public void Clear()
        {
            _lines.Clear();
            if (enabled) _text.text = "";
        }

        public void Append(RulesEngine e, IReadOnlyList<RuleEvent> events)
        {
            if (!enabled) return;
            foreach (var ev in events)
            {
                var line = RunText.Describe(ev, e.Config.Height);
                if (line != null) _lines.Add(line);
            }
            while (_lines.Count > _maxLines) _lines.RemoveAt(0);
            var sb = new StringBuilder();
            for (int i = 0; i < _lines.Count; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append(_lines[i]);
            }
            _text.text = sb.ToString();
        }
    }
}
