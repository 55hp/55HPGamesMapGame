using UnityEngine;
using UnityEngine.UI;
using TMPro;
using hp55games.Mobile.Core.UI;
using hp55games.MapGame.Features.Gameplay.HexGrid;

namespace hp55games.MapGame.Features.UI
{
    /// <summary>
    /// Popup di incontro Enemy/Miniboss. Istanziato da RevealEffectPopupDispatcher via
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
        [SerializeField] private Image _background;

        [Header("Configurazione visiva condivisa")]
        [Tooltip("Stesso catalogo usato da HexTileView — così _monsterIcon mostra la stessa icona della tile.")]
        [SerializeField] private HexTileConfigCatalog _visualConfig;

        [Header("Bottoni")]
        [SerializeField] private Button _fightButton;
        [SerializeField] private Button _fleeButton;

        private HexGridController _grid;

        /// <summary>Chiamato da RevealEffectPopupDispatcher subito dopo l'istanziazione.</summary>
        public void Open(HexTileData tile, HexGridController grid)
        {
            _grid = grid;

            // Stessa Label mostrata da HexTileView (_iconLabel) per questo tipo — vedi
            // _visualConfig sopra. Se il catalogo non e' assegnato o la Label e' vuota,
            // fallback sul nome enum grezzo (comportamento precedente).
            if (_tileTypeLabel != null)
            {
                var label = _visualConfig != null ? _visualConfig.GetLabel(tile.Type) : null;
                _tileTypeLabel.text = !string.IsNullOrEmpty(label) ? label : tile.Type.ToString();
            }

            if (_difficultyLabel != null)
            {
                _difficultyLabel.text = $"Livello {tile.DifficultyLevel}";

                // Stesso colore usato da HexTileView per il bordo (SetDifficultyLevel) —
                // Color.clear se il DifficultyLevel non ha ancora un colore assegnato nel
                // catalogo, cosi' un valore mancante resta visibilmente "mancante".
                if (_visualConfig != null)
                    _difficultyLabel.color = _visualConfig.GetDifficultyLevelColor(tile.DifficultyLevel);
            }

            // Stessa icona mostrata da HexTileView per questo tipo. Se il catalogo non e'
            // assegnato o il tipo non ha ancora una HexTileConfig, resta il placeholder
            // assegnato in Inspector.
            if (_monsterIcon != null)
            {
                var icon = _visualConfig != null ? _visualConfig.GetIcon(tile.Type) : null;
                if (icon != null)
                    _monsterIcon.sprite = icon;
            }

            // Colore per TileType — stessa tavolozza usata da MapGenerationConfigEditor
            // per la preview (vedi TileTypePalette, unica fonte per i due).
            if (_background != null)
                _background.color = TileTypePalette.GetColor(tile.Type);

            Bind(_fightButton, OnFightClicked);
            Bind(_fleeButton, OnFleeClicked);

            // Gate fuga: fuggire richiede Cibo >= 2. Con Cibo insufficiente il bottone
            // appare ma e' disattivato — l'unica opzione e' combattere.
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
