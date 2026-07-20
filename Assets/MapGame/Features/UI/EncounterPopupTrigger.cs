using System.Threading.Tasks;
using UnityEngine;
using hp55games.Mobile.Core;
using hp55games.Mobile.Core.Architecture;
using hp55games.Mobile.Core.UI;
using hp55games.MapGame.Features.Gameplay.HexGrid;

namespace hp55games.MapGame.Features.UI
{
    /// <summary>
    /// Ascolta HexGridController.EncounterStarted e apre UIPopup_Encounter via
    /// IUIPopupService. Nessuna logica di gameplay qui, solo il collegamento
    /// evento -> apertura popup (stesso spirito di HexTileTapController per l'input).
    /// </summary>
    public sealed class EncounterPopupTrigger : MonoBehaviour
    {
        [SerializeField] private HexGridController _grid;

        private IUIPopupService _popupService;

        private void Awake()
        {
            _popupService = ServiceRegistry.Resolve<IUIPopupService>();
        }

        private void OnEnable()
        {
            if (_grid == null)
            {
                Debug.LogError("[EncounterPopupTrigger] _grid non assegnato.", this);
                return;
            }

            _grid.EncounterStarted += OnEncounterStarted;
        }

        private void OnDisable()
        {
            if (_grid != null)
                _grid.EncounterStarted -= OnEncounterStarted;
        }

        private void OnEncounterStarted(HexTileData tile)
        {
            AsyncUtils.FireAndForget(OpenPopupAsync(tile), context: nameof(EncounterPopupTrigger));
        }

        private async Task OpenPopupAsync(HexTileData tile)
        {
            var popup = await _popupService.OpenAsync<UIPopup_Encounter>(Addr.Content.UI.Popups.Popup_Encounter);
            if (popup == null)
            {
                Debug.LogError("[EncounterPopupTrigger] Impossibile aprire UIPopup_Encounter (address non registrato o Addressables non buildate).");
                return;
            }

            popup.Open(tile, _grid);
        }
    }
}
