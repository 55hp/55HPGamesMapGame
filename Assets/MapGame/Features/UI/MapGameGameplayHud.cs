using System;
using System.Collections.Generic;
using hp55games.Mobile.Core.Architecture;
using hp55games.Mobile.Core.Context;
using hp55games.Mobile.Core.Gameplay.Events;
using UnityEngine;

namespace hp55games.MapGame.Features.UI
{
    /// <summary>
    /// HUD specifico di MapGame per HP e Cibo, in stile "stack di icone" (cuori/mele).
    /// Riscritto 2026-07-17: non usa più liste di GameObject pre-piazzati. Istanzia i
    /// prefab a runtime dentro i container e mantiene visibile esattamente un numero di
    /// icone pari al valore corrente (_context.Lives / _context.Food). L'ordine delle
    /// icone non è significativo: i prefab sono identici, si accendono/spengono le prime
    /// N del pool interno.
    ///
    /// Il massimo mostrabile non è più cablato qui: il pool cresce da solo quando il
    /// valore corrente supera il numero di icone già istanziate (es. cura oltre lo start,
    /// o futuro aumento di MaxHp da leveling). Nessun sync manuale con SurvivalConfig.
    ///
    /// UIGameplayHUD (Core, generico) resta responsabile di Monete/Score, non toccato qui.
    /// </summary>
    public sealed class MapGameGameplayHud : MonoBehaviour
    {
        [Header("Cuori (HP)")]
        [Tooltip("Contenitore (es. un Horizontal Layout Group) dentro cui istanziare i cuori. Deve partire vuoto.")]
        [SerializeField] private Transform _heartsContainer;
        [Tooltip("Prefab di un singolo cuore.")]
        [SerializeField] private GameObject _heartPrefab;

        [Header("Mele (Cibo)")]
        [Tooltip("Contenitore dentro cui istanziare le mele. Deve partire vuoto.")]
        [SerializeField] private Transform _foodContainer;
        [Tooltip("Prefab di una singola mela.")]
        [SerializeField] private GameObject _foodPrefab;

        private readonly List<GameObject> _heartPool = new();
        private readonly List<GameObject> _foodPool = new();

        private IGameContextService _context;
        private IEventBus _bus;

        private IDisposable _hpSub;
        private IDisposable _foodSub;

        private void Awake()
        {
            _context = ServiceRegistry.Resolve<IGameContextService>();
            _bus     = ServiceRegistry.Resolve<IEventBus>();

            if (_bus != null)
            {
                _hpSub   = _bus.Subscribe<HpChangedEvent>(OnHpChanged);
                _foodSub = _bus.Subscribe<FoodChangedEvent>(OnFoodChanged);
            }
        }

        private void Start()
        {
            // Stato iniziale: nel caso l'evento di init sia già stato pubblicato prima che
            // questo componente si iscrivesse.
            Refresh();
        }

        private void OnDestroy()
        {
            _hpSub?.Dispose();
            _foodSub?.Dispose();
        }

        private void Refresh()
        {
            SyncPool(_heartPool, _heartsContainer, _heartPrefab, _context?.Lives ?? 0);
            SyncPool(_foodPool,  _foodContainer,   _foodPrefab,   _context?.Food  ?? 0);
        }

        private void OnHpChanged(HpChangedEvent _)     => SyncPool(_heartPool, _heartsContainer, _heartPrefab, _context.Lives);
        private void OnFoodChanged(FoodChangedEvent _) => SyncPool(_foodPool,  _foodContainer,   _foodPrefab,   _context.Food);

        /// <summary>
        /// Fa in modo che nel container siano visibili esattamente `count` icone. Il pool
        /// cresce istanziando nuovi prefab quando servono, altrimenti riusa quelli già
        /// creati accendendo/spegnendo (nessuna Destroy, nessun GC dopo il warmup).
        /// count negativo (sentinel di pre-init) viene clampato a 0: tutto spento.
        /// </summary>
        private static void SyncPool(List<GameObject> pool, Transform container, GameObject prefab, int count)
        {
            if (container == null || prefab == null) return;
            count = Mathf.Max(0, count);

            while (pool.Count < count)
                pool.Add(Instantiate(prefab, container));

            for (int i = 0; i < pool.Count; i++)
                pool[i].SetActive(i < count);
        }
    }
}
