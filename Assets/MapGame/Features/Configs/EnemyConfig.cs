using hp55games.Mobile.Core.Config;
using UnityEngine;

namespace hp55games.MapGame.Features.Configs
{
    /// <summary>
    /// Parametri di bilanciamento del nemico/combattimento (separato da TraderConfig, che
    /// resta lo shop). Stesso pattern di SurvivalConfig: risolto a runtime via
    /// IConfigCatalogService, il bilanciamento si fa sull'asset in editor, mai nel codice.
    ///
    /// I valori di default sono segnaposto NON bilanciati. ATK/DEF e altri parametri di
    /// combattimento arriveranno quando esistera' il sistema Combattimento.
    /// </summary>
    [CreateAssetMenu(menuName = "MapGame/Enemy Config", fileName = "EnemyConfig")]
    public sealed class EnemyConfig : ScriptableObject, IConfigAsset
    {
        [Header("Ricompensa Combattimento")]
        [Tooltip("Moltiplicatore applicato al DifficultyLevel dell'Enemy per calcolare le Monete guadagnate in ResolveEncounterFight (arrotondato all'intero piu' vicino).")]
        [Min(0f)] public float EnemyKillCoinMultiplier = 1f;

        private void OnValidate()
        {
            EnemyKillCoinMultiplier = Mathf.Max(0f, EnemyKillCoinMultiplier);
        }
    }
}
