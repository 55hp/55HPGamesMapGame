using UnityEngine;
using hp55games.Mobile.Core.Architecture;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Registra IMapGenerationService in ServiceRegistry quando la scena Menu (01_Menu)
    /// viene caricata — prima che 02_Gameplay carichi GridHandler/HexGridController,
    /// che lo risolve in Awake(). Stesso pattern di SceneFlowServiceInstaller.
    /// </summary>
    public sealed class MapGenerationServiceInstaller : MonoBehaviour
    {
        private void Awake()
        {
            ServiceRegistry.Register<IMapGenerationService>(new MapGenerationService());
            Debug.Log("[MapGeneration] MapGenerationService registered.");
        }
    }
}