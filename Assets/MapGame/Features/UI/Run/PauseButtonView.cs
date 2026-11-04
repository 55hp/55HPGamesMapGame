using hp55games.Mobile.Core;
using hp55games.Mobile.Core.Architecture;
using hp55games.Mobile.Core.SceneFlow;
using UnityEngine;
using UnityEngine.UI;

namespace hp55games.MapGame.Features.UI.Run
{
    /// <summary>
    /// Pulsante pausa dell'HUD di partita: passa allo stato Pausa del template (popup Pausa: Riprendi / Esci).
    /// </summary>
    public sealed class PauseButtonView : MonoBehaviour
    {
        [SerializeField] private Button _button;

        private ISceneFlowService _sceneFlow;

        private void Awake()
        {
            if (_button == null)
            {
                Debug.LogError("[PauseButtonView] _button non assegnato nel prefab.", this);
                enabled = false;
                return;
            }
            if (!ServiceRegistry.TryResolve(out _sceneFlow))
            {
                Debug.LogError("[PauseButtonView] ISceneFlowService non registrato.", this);
                enabled = false;
                return;
            }
            _button.onClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked()
        {
            if (!enabled) return;
            AsyncUtils.FireAndForget(_sceneFlow.GoToPauseAsync(), context: nameof(PauseButtonView));
        }
    }
}
