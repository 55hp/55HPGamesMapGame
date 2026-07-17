using hp55games.Mobile.Core.Architecture;
using UnityEngine;

namespace hp55games.Mobile.Core.Config
{
    /// <summary>
    /// Registra IConfigCatalogService in ServiceRegistry. Va posto in una scena che carica
    /// PRIMA dei consumer (es. Bootstrap / 01_Menu), stesso pattern e timing di
    /// MapGenerationServiceInstaller: HexGridController risolve il servizio in Awake() nella
    /// scena di gameplay, quindi il catalogo deve essere gia' registrato a quel punto.
    /// </summary>
    public sealed class ConfigCatalogInstaller : MonoBehaviour
    {
        [Tooltip("Il ConfigCatalog popolato via scan. Trascina qui l'asset.")]
        [SerializeField] private ConfigCatalog _catalog;

        private void Awake()
        {
            if (_catalog == null)
            {
                Debug.LogError("[ConfigCatalogInstaller] _catalog non assegnato. Assegna il ConfigCatalog nell'Inspector.", this);
                return;
            }

            ServiceRegistry.Register<IConfigCatalogService>(new ConfigCatalogService(_catalog));
            Debug.Log("[Config] ConfigCatalogService registered.");
        }
    }
}
