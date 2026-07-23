using UnityEngine;
using UnityEngine.UI;
using TMPro;
using hp55games.Mobile.Core.UI;
using hp55games.MapGame.Features.Gameplay.HexGrid;

namespace hp55games.MapGame.Features.UI
{
    /// <summary>
    /// Popup di incontro Enemy/Miniboss. Istanziato da EncounterPopupTrigger via
    /// IUIPopupService quando HexGridController pubblica EncounterStarted. Mostra
    /// tile.Type e tile.DifficultyLevel come placeholder di info nemico (nessun dato
    /// di lore/nome esiste ancora, non aggiungerne). Bottone Combatti chiama
    /// ResolveEncounterFight(), bottone Fuggi chiama ResolveEncounterFlee(); in
    /// entrambi i casi il popup si chiude subito dopo tramite ClosePopup() (UIPopupBase).
    /// </summary>
    public sealed class UIPopup_Encounter : UIPopupBase
    {
        [Header("Info nemico")]
        [SerializeField] private TextMeshProUGUI _tileTypeLabel;
        [SerializeField] private TextMeshProUGUI _difficultyLabel;
        [SerializeField] private Image _monsterIcon;

        [Header("Bottoni")]
        [SerializeField] private Button _fightButton;
        [SerializeField] private Button _fleeButton;

        private HexGridController _grid;

        /// <summary>Chiamato da EncounterPopupTrigger subito dopo l'istanziazione.</summary>
        public void Open(HexTileData tile, HexGridController grid)
        {
            _grid = grid;

            if (_tileTypeLabel != null)
                _tileTypeLabel.text = tile.Type.ToString();

            if (_difficultyLabel != null)
                _difficultyLabel.text = $"Livello {tile.DifficultyLevel}";

            // _monsterIcon.sprite resta il placeholder assegnato in Inspector finché non
            // esiste arte per tipo/DifficultyLevel (vedi Asset Inventory in Notion).

            Bind(_fightButton, OnFightClicked);
            Bind(_fleeButton, OnFleeClicked);

            // Gate fuga (2026-07-23): fuggire richiede Cibo >= 2. Con Cibo insufficiente
            // il bottone appare ma e' disattivato — l'unica opzione e' combattere.
            // ResolveEncounterFlee applica lo stesso gate come difesa in profondita'.
            if (_fleeButton != null)
                _fleeButton.interactable = grid.CanFlee;
        }

        private void OnFightClicked()
        {
            _grid.ResolveEncounterFight();
            ClosePopup();
        }

        private void OnFleeClicked()
        {
            _grid.ResolveEncounterFlee();
            ClosePopup();
        }
    }
}
