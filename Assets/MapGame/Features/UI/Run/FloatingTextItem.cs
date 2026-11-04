using TMPro;
using UnityEngine;

namespace hp55games.MapGame.Features.UI.Run
{
    /// <summary>Singolo numero fluttuante (prefab): testo + CanvasGroup per la dissolvenza. Lo anima FloatingTextView.</summary>
    public sealed class FloatingTextItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private CanvasGroup _group;

        public TextMeshProUGUI Text => _text;
        public CanvasGroup Group => _group;
        public RectTransform Rect => (RectTransform)transform;
        public bool IsValid => _text != null && _group != null;
    }
}
