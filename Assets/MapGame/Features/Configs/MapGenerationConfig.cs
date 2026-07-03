using UnityEngine;

namespace hp55games.MapGame.Features.Configs
{
    /// <summary>
    /// Configurazione ScriptableObject per la generazione della mappa.
    /// Crea istanze via Assets > Create > MapGame > Map Generation Config.
    /// Salva i file .asset in Assets/MapGame/Content/Configs/ per separare
    /// il codice (Features) dai dati (Content).
    /// </summary>
    [CreateAssetMenu(menuName = "MapGame/Map Generation Config", fileName = "MapGenerationConfig")]
    public sealed class MapGenerationConfig : ScriptableObject
    {
        [Min(1)] public int Width  = 10;
        [Min(1)] public int Height = 10;

        /// <summary>
        /// Seed di default per la generazione.
        /// A runtime viene sovrascritto da IGameContextService.CurrentRunSeed se != 0.
        /// </summary>
        public int Seed = 12345;
    }
}
