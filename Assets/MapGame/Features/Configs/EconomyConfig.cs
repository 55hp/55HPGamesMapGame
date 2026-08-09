using hp55games.Mobile.Core.Config;
using UnityEngine;

namespace hp55games.MapGame.Features.Configs
{
    /// <summary>
    /// Parametri dell'economia Monete: prezzi ed entita' degli effetti dei potenziamenti
    /// acquistabili nello shop. Ricomprabili nella stessa run. Stesso pattern di
    /// SurvivalConfig: risolto a runtime via IConfigCatalogService, il bilanciamento si fa
    /// sull'asset in editor, mai nel codice.
    ///
    /// I valori di default sono segnaposto NON bilanciati. ATK/DEF, chiavi e vision boost
    /// arriveranno quando esisteranno i sistemi che li consumano (Combattimento / Knowledge).
    ///
    /// Include anche EnemyKillCoinMultiplier, che scala la ricompensa in Monete del
    /// combattimento (DifficultyLevel * multiplier, vedi ResolveEncounterFight).
    /// </summary>
    [CreateAssetMenu(menuName = "MapGame/Economy Config", fileName = "EconomyConfig")]
    public sealed class EconomyConfig : ScriptableObject, IConfigAsset
    {
        [Header("Potenziamento cap HP")]
        [Min(1)] public int MaxHpUpgradeCost = 10;
        [Min(1)] public int MaxHpUpgradeAmount = 1;

        [Header("Potenziamento slot Cibo")]
        [Min(1)] public int FoodSlotUpgradeCost = 10;
        [Min(1)] public int FoodSlotUpgradeAmount = 1;

        [Header("Cura HP")]
        [Min(1)] public int HealCost = 5;
        [Min(1)] public int HealAmount = 2;

        [Header("Rifornimento Cibo")]
        [Min(1)] public int FoodRefillCost = 5;
        [Min(1)] public int FoodRefillAmount = 2;

        [Header("Ricompensa Combattimento")]
        [Tooltip("Moltiplicatore applicato al DifficultyLevel dell'Enemy per calcolare le Monete guadagnate in ResolveEncounterFight (arrotondato all'intero piu' vicino).")]
        [Min(0f)] public float EnemyKillCoinMultiplier = 1f;

        private void OnValidate()
        {
            MaxHpUpgradeCost = Mathf.Max(1, MaxHpUpgradeCost);
            MaxHpUpgradeAmount = Mathf.Max(0, MaxHpUpgradeAmount);
            FoodSlotUpgradeCost = Mathf.Max(1, FoodSlotUpgradeCost);
            FoodSlotUpgradeAmount = Mathf.Max(0, FoodSlotUpgradeAmount);
            HealCost = Mathf.Max(1, HealCost);
            HealAmount = Mathf.Max(0, HealAmount);
            FoodRefillCost = Mathf.Max(1, FoodRefillCost);
            FoodRefillAmount = Mathf.Max(0, FoodRefillAmount);
            EnemyKillCoinMultiplier = Mathf.Max(0f, EnemyKillCoinMultiplier);
        }
    }
}

