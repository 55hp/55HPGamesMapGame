using System;
using System.Collections.Generic;
using hp55games.Mobile.Core.Architecture;
using hp55games.Mobile.Core.Context;
using hp55games.Mobile.Core.Gameplay.Events;
using UnityEngine;

namespace hp55games.MapGame.Features.UI
{
    /// <summary>
    /// HUD specifico di MapGame per HP e Cibo, in stile "stack di icone" (cuori/mele)
    /// invece del testo numerico usato da UIGameplayHUD (Core, generico — resta
    /// responsabile di Monete/Score, non toccato da questo script).
    ///
    /// _hearts e _food sono liste di GameObject pre-piazzati nel prefab (uno per HP/Cibo
    /// massimo). Vengono trattate come uno stack: ad ogni cambiamento si accendono le
    /// prime N (N = valore corrente) e si spengono le restanti, in ordine di lista —
    /// index 0 è il primo ad accendersi e l'ultimo a spegnersi.
    ///
    /// Il numero di elementi in ciascuna lista definisce implicitamente il massimo
    /// mostrabile: se _context.Lives/_context.Food supera la lunghezza della lista il
    /// valore in eccesso viene semplicemente ignorato (clamp), non aggiunge icone.
    /// Tienilo allineato a HexGridController._maxHp / _maxFood quando li cambi.
    /// </summary>
    public sealed class MapGameGameplayHud : MonoBehaviour
    {
        [Header("Cuori (HP) — uno per punto vita massimo")]
        [SerializeField] private List<GameObject> _hearts;

        [Header("Mele (Cibo) — uno per slot cibo massimo")]
        [SerializeField] private List<GameObject> _food;

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
            // Stato iniziale: in caso l'evento di init sia già stato pubblicato prima che
            // questo componente si iscrivesse (stesso ordine di Awake/Start usato da
            // UIGameplayHUD, vedi Init() lì).
            Refresh();
        }

        private void OnDestroy()
        {
            _hpSub?.Dispose();
            _foodSub?.Dispose();
        }

        private void Refresh()
        {
            ApplyStack(_hearts, _context?.Lives ?? -1);
            ApplyStack(_food,   _context?.Food  ?? -1);
        }

        private void OnHpChanged(HpChangedEvent _)     => ApplyStack(_hearts, _context.Lives);
        private void OnFoodChanged(FoodChangedEvent _) => ApplyStack(_food,   _context.Food);

        /// <summary>
        /// Accende stack[0..count-1], spegne il resto. count viene clampato tra 0 e
        /// stack.Count, così un valore negativo (Lives/Food non ancora inizializzati,
        /// sentinel -1) spegne semplicemente tutta la fila senza errori.
        /// </summary>
        private static void ApplyStack(List<GameObject> stack, int count)
        {
            if (stack == null) return;

            int clamped = Mathf.Clamp(count, 0, stack.Count);
            for (int i = 0; i < stack.Count; i++)
            {
                if (stack[i] != null)
                    stack[i].SetActive(i < clamped);
            }
        }
    }
}
