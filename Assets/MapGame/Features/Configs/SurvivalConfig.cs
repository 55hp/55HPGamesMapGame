using hp55games.Mobile.Core.Config;
using UnityEngine;

namespace hp55games.MapGame.Features.Configs
{
    /// <summary>
    /// Parametri di sopravvivenza del giocatore, centralizzati per iterare sul design senza
    /// toccare la scena. Sostituisce i campi _maxHp/_maxFood/_noFoodHpPenalty che vivevano
    /// direttamente su HexGridController. Risolto a runtime via IConfigCatalogService.
    ///
    /// Start e Max sono separati: la sessione parte da StartHp/StartFood, i clamp usano
    /// MaxHp/MaxFood. Quando arrivera' il leveling (+1 MaxHp per livello) alzera' MaxHp a
    /// runtime mentre StartHp resta il valore di partenza.
    /// </summary>
    [CreateAssetMenu(menuName = "MapGame/Survival Config", fileName = "SurvivalConfig")]
    public sealed class SurvivalConfig : ScriptableObject, IConfigAsset
    {
        [Header("Punti Vita")]
        [Min(1)] public int StartHp = 5;
        [Min(1)] public int MaxHp = 5;

        [Header("Cibo")]
        [Min(0)] public int StartFood = 3;
        [Min(0)] public int MaxFood = 3;

        [Header("Costo movimento")]
        [Tooltip("Cibo consumato a ogni click, se disponibile.")]
        [Min(0)] public int FoodCostPerClick = 1;

        [Tooltip("HP persi in un click quando il Cibo e' gia' a 0.")]
        [Min(0)] public int NoFoodHpPenalty = 2;

        private void OnValidate()
        {
            if (StartHp > MaxHp) StartHp = MaxHp;
            if (StartFood > MaxFood) StartFood = MaxFood;
        }
    }
}
