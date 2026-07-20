using UnityEngine;
using UnityEngine.UI;
using hp55games.Mobile.Core.Architecture;
using hp55games.Mobile.Core.UI;
using hp55games.MapGame.Features.Gameplay.HexGrid;
using TMPro;

namespace hp55games.MapGame.Features.UI
{
    /// <summary>
    /// Popup di incontro Enemy/Miniboss. Istanziato da EncounterPopupTrigger via
    /// IUIPopupService quando HexGridController pubblica EncounterStarted. Mostra
    /// tile.Type e tile.DifficultyLevel come placeholder di info nemico (nessun dato
    /// di lore/nome esiste ancora, non aggiungerne). Bottone Combatti chiama
    /// ResolveEncounterFight(), bottone Fuggi chiama ResolveEncounterFlee(); in
    /// entrambi i casi il popup si chiude subito dopo tramite IUIPopupService.Close.
    /// </summary>
    public sealed class UIPopup_Encounter : MonoBehaviour
    {
        [Header("Info nemico")]
        [SerializeField] private TextMeshProUGUI _tileTypeLabel;
        [SerializeField] private TextMeshProUGUI _difficultyLabel;
        [SerializeField] private Image _monsterIcon;

        [Header("Bottoni")]
        [SerializeField] private Button _fightButton;
        [SerializeField] private Button _fleeButton;

        private HexGridController _grid;
        private IUIPopupService    _popupService;

        /// <summary>Chiamato da EncounterPopupTrigger subito dopo l'istanziazione.</summary>
        public void Open(HexTileData tile, HexGridController grid)
        {
            _grid         = grid;
            _popupService = ServiceRegistry.Resolve<IUIPopupService>();

            if (_tileTypeLabel != null)
                _tileTypeLabel.text = tile.Type.ToString();

            if (_difficultyLabel != null)
                _difficultyLabel.text = $"Livello {tile.DifficultyLevel}";

            // _monsterIcon.sprite resta il placeholder assegnato in Inspector finché non
            // esiste arte per tipo/DifficultyLevel (vedi Asset Inventory in Notion).

            _fightButton.onClick.RemoveAllListeners();
            _fightButton.onClick.AddListener(OnFightClicked);

            _fleeButton.onClick.RemoveAllListeners();
            _fleeButton.onClick.AddListener(OnFleeClicked);
        }

        private void OnFightClicked()
        {
            _grid.ResolveEncounterFight();
            Close();
        }

        private void OnFleeClicked()
        {
            _grid.ResolveEncounterFlee();
            Close();
        }

        private void Close()
        {
            _popupService?.Close(gameObject);
        }
    }
}