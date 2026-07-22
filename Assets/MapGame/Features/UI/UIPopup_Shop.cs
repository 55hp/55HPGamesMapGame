using UnityEngine;
using UnityEngine.UI;
using TMPro;
using hp55games.Mobile.Core.UI;
using hp55games.MapGame.Features.Gameplay.HexGrid;

namespace hp55games.MapGame.Features.UI
{
    /// <summary>
    /// Popup dello shop (Economia, 2026-07-20): potenziamenti diretti in Monete,
    /// ricomprabili nella stessa run. Chiama le TryBuy* di HexGridController — prezzi e
    /// logica vivono la' (config EconomyConfig), qui solo la presentazione. I bottoni
    /// restano sempre attivi: un acquisto impossibile (Monete insufficienti, cura a HP
    /// pieni) e' un TryBuy che ritorna false, mostrato nel label esito. Resta aperto dopo
    /// ogni acquisto (ricomprabile), si chiude col bottone dedicato.
    ///
    /// I testi dei prezzi vanno impostati sul prefab a mano (placeholder): leggere i
    /// valori da EconomyConfig a runtime richiederebbe di esporre il config fuori da
    /// HexGridController — da valutare quando lo shop diventera' UI definitiva.
    /// </summary>
    public sealed class UIPopup_Shop : UIPopupBase
    {
        [Header("Monete")]
        [SerializeField] private TextMeshProUGUI _moneteLabel;

        [Header("Esito ultimo acquisto")]
        [SerializeField] private TextMeshProUGUI _resultLabel;

        [Header("Bottoni acquisto")]
        [SerializeField] private Button _maxHpButton;
        [SerializeField] private Button _foodSlotButton;
        [SerializeField] private Button _healButton;
        [SerializeField] private Button _foodRefillButton;

        [Header("Chiusura")]
        [SerializeField] private Button _closeButton;

        private HexGridController _grid;

        /// <summary>Chiamato da chi apre il popup subito dopo l'istanziazione.</summary>
        public void Open(HexGridController grid)
        {
            _grid = grid;

            Bind(_maxHpButton,      () => Purchase(_grid.TryBuyMaxHpUpgrade(),  "Cap HP aumentato"));
            Bind(_foodSlotButton,   () => Purchase(_grid.TryBuyFoodSlotUpgrade(), "Slot Cibo aumentato"));
            Bind(_healButton,       () => Purchase(_grid.TryBuyHeal(),          "HP curati"));
            Bind(_foodRefillButton, () => Purchase(_grid.TryBuyFoodRefill(),    "Cibo rifornito"));
            Bind(_closeButton,      ClosePopup);

            if (_resultLabel != null)
                _resultLabel.text = "";

            RefreshMonete();
        }

        private void Purchase(bool success, string successLabel)
        {
            if (_resultLabel != null)
                _resultLabel.text = success ? successLabel : "Acquisto non possibile";

            RefreshMonete();
        }

        private void RefreshMonete()
        {
            if (_moneteLabel != null && _grid != null)
                _moneteLabel.text = $"Monete: {ResolveMonete()}";
        }

        private int ResolveMonete()
        {
            // Lo Score vive in IGameContextService, ma il popup non ha bisogno di
            // risolverlo: HexGridController e' gia' la facciata dello stato di run.
            return _grid.CurrentMonete;
        }
    }
}
