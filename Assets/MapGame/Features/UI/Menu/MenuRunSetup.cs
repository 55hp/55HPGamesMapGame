using hp55games.MapGame.Features.Gameplay.Run;
using hp55games.Mobile.Core.Architecture;
using UnityEngine;

namespace hp55games.MapGame.Features.UI.Menu
{
    /// <summary>
    /// Sulla pagina del menu: a ogni apertura riporta le impostazioni della prossima partita al default
    /// (modalità classica, nessun seed). I pulsanti di modalità e "Mappa del giorno" le cambiano dopo.
    /// "Riprova" dai risultati non passa dal menu, quindi conserva la modalità.
    /// </summary>
    public sealed class MenuRunSetup : MonoBehaviour
    {
        private void OnEnable() => Get().ResetToDefault();

        public static RunSeedRequest Get()
        {
            if (!ServiceRegistry.TryResolve(out RunSeedRequest request) || request == null)
            {
                request = new RunSeedRequest();
                ServiceRegistry.Register(request);
            }
            return request;
        }
    }
}
