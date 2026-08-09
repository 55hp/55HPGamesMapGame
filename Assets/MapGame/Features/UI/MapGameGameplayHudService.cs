using System.Collections;
using hp55games.Mobile.Core.Architecture;
using hp55games.Ui;
using UnityEngine;

namespace hp55games.MapGame.Features.UI
{
    public interface IMapGameGameplayHudService
    {
        GameObject HudInstance { get; }
    }

    public sealed class MapGameGameplayHudService : MonoBehaviour, IMapGameGameplayHudService
    {
        [SerializeField] private GameObject _hudPrefab;

        public GameObject HudInstance { get; private set; }

        private void Awake()
        {
            ServiceRegistry.Register<IMapGameGameplayHudService>(this);
        }

        private void Start()
        {
            StartCoroutine(EnsureHudAsync());
        }

        private IEnumerator EnsureHudAsync()
        {
            UIRoot ui = null;
            for (var i = 0; i < 120 && ui == null; i++)
            {
                ui = UIRoot.FindOrCache();
                if (ui == null) yield return null;
            }

            if (ui == null || ui.hud == null)
            {
                Debug.LogError("[MapGameGameplayHudService] UIRoot.hud non trovato.");
                yield break;
            }

            if (_hudPrefab == null)
            {
                Debug.LogError("[MapGameGameplayHudService] UIGameHUD prefab non assegnato.");
                yield break;
            }

            var existing = ui.hud.Find(_hudPrefab.name);
            if (existing != null)
            {
                HudInstance = existing.gameObject;
                yield break;
            }

            HudInstance = Instantiate(_hudPrefab, ui.hud, false);
            HudInstance.name = _hudPrefab.name;
        }

        private void OnDestroy()
        {
            if (HudInstance != null && HudInstance.transform.parent != null)
                Destroy(HudInstance);
        }
    }
}
