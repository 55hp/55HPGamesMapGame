using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace hp55games.MapGame.Features.UI.Run
{
    /// <summary>Etichetta di riga/colonna ai bordi della griglia (prefab assegnato a BoardLineLabelsView).</summary>
    public sealed class LineLabelView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _label;
        [Tooltip("Risultato dell'indizio sulla linea (sigla + numero). Si nasconde se la linea non è usata.")]
        [SerializeField] private TextMeshProUGUI _clue;
        [Tooltip("Evidenzia le linee selezionabili mentre si posa un indizio.")]
        [SerializeField] private GameObject _selectableHighlight;

        private Action _onClick;

        public RectTransform Rect => (RectTransform)transform;

        private void Awake()
        {
            if (_button == null || _label == null || _clue == null || _selectableHighlight == null)
            {
                Debug.LogError("[LineLabelView] Riferimenti mancanti nel prefab.", this);
                enabled = false;
                return;
            }
            _button.onClick.AddListener(() => _onClick?.Invoke());
        }

        public void Set(string label, string clue, bool selectable, Action onClick)
        {
            _label.text = label;
            _clue.text = clue;
            _clue.gameObject.SetActive(!string.IsNullOrEmpty(clue));
            _selectableHighlight.SetActive(selectable);
            _button.interactable = selectable;
            _onClick = onClick;
        }
    }
}
