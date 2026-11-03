using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace hp55games.MapGame.Features.UI.Run
{
    /// <summary>Riga-pulsante di un'opzione (prefab assegnato a DecisionPanelView).</summary>
    public sealed class DecisionOptionView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _label;
        [Tooltip("Riga secondaria: prezzo, motivo, rischio. Si nasconde se vuota.")]
        [SerializeField] private TextMeshProUGUI _detail;

        private Action _onClick;

        private void Awake()
        {
            if (_button == null || _label == null || _detail == null)
            {
                Debug.LogError("[DecisionOptionView] _button, _label o _detail non assegnati nel prefab.", this);
                enabled = false;
                return;
            }
            _button.onClick.AddListener(() => _onClick?.Invoke());
        }

        public void Set(string label, string detail, bool interactable, Action onClick)
        {
            _label.text = label;
            _detail.text = detail;
            _detail.gameObject.SetActive(!string.IsNullOrEmpty(detail));
            _button.interactable = interactable;
            _onClick = onClick;
        }
    }
}
