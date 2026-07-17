using System;
using System.Collections.Generic;
using hp55games.Mobile.Core.Architecture;
using hp55games.Mobile.Core.Context;
using hp55games.Mobile.Core.Gameplay.Events;
using UnityEngine;
using UnityEngine.UI;

namespace hp55games.MapGame.Features.UI
{
    /// <summary>
    /// HUD specifico di MapGame per HP, Cibo, XP e Level Up, in stile "stack di icone"
    /// (cuori/mele/pallini) invece del testo numerico usato da UIGameplayHUD (Core,
    /// generico — resta responsabile solo di Monete/Score, non toccato da questo script).
    ///
    /// Icone costruite dinamicamente da prefab, non più pre-piazzate a mano in Inspector:
    /// il numero di cuori/mele istanziati segue HexGridController.MaxHp/MaxFood in tempo
    /// reale, così il tuning (HP/Cibo iniziali, crescita per level up) non richiede più
    /// toccare l'HUD — cambi un numero su HexGridController e l'HUD si aggiorna da sé.
    ///
    /// _xpDots ha sempre HexGridController.XpPerLevel - 1 (9) elementi: rappresenta Xp da
    /// 1 a 9. Quando Xp arriva al massimo (10) i pallini si spengono tutti invece di
    /// restare pieni — il segnale "pronto al level up" passa interamente al bottone.
    ///
    /// _grid (HexGridController) non è un riferimento di scena: l'HUD è caricata a
    /// runtime via IUINavigationService, non esiste nella scena all'edit time, quindi non
    /// si può trascinare in Inspector. HexGridController si auto-registra in
    /// ServiceRegistry al proprio Awake() (stesso pattern di FeedbackService), qui lo
    /// risolviamo da lì.
    /// </summary>
    public sealed class MapGameGameplayHud : MonoBehaviour
    {
        [Header("Cuori (HP)")]
        [SerializeField] private GameObject _heartPrefab;
        [SerializeField] private Transform _heartsContainer;

        [Header("Mele (Cibo)")]
        [SerializeField] private GameObject _foodPrefab;
        [SerializeField] private Transform _foodContainer;

        [Header("Pallini XP (Xp 1-9, il 10° è solo il bottone)")]
        [SerializeField] private GameObject _xpDotPrefab;
        [SerializeField] private Transform _xpDotsContainer;

        [Header("Level Up")]
        [SerializeField] private Button _levelUpButton;

        private hp55games.MapGame.Features.Gameplay.HexGrid.HexGridController _grid;
        private IGameContextService _context;
        private IEventBus _bus;

        private readonly List<GameObject> _heartInstances = new();
        private readonly List<GameObject> _foodInstances  = new();
        private readonly List<GameObject> _xpDotInstances = new();

        private IDisposable _hpSub;
        private IDisposable _foodSub;
        private IDisposable _xpSub;

        private void Awake()
        {
            _context = ServiceRegistry.Resolve<IGameContextService>();
            _bus     = ServiceRegistry.Resolve<IEventBus>();

            if (!ServiceRegistry.TryResolve(out _grid))
                Debug.LogWarning("[MapGameGameplayHud] HexGridController non trovato in ServiceRegistry: HUD e bottone Level Up non funzioneranno.", this);

            if (_bus != null)
            {
                _hpSub   = _bus.Subscribe<HpChangedEvent>(OnHpChanged);
                _foodSub = _bus.Subscribe<FoodChangedEvent>(OnFoodChanged);
                _xpSub   = _bus.Subscribe<XpChangedEvent>(OnXpChanged);
            }

            if (_levelUpButton != null)
                _levelUpButton.onClick.AddListener(OnLevelUpClicked);
        }

        private void Start()
        {
            // Stato iniziale: in caso l'evento di init sia già stato pubblicato prima che
            // questo componente si iscrivesse (stesso ordine di Awake/Start usato da
            // UIGameplayHUD, vedi Init() lì).
            RefreshHearts();
            RefreshFood();
            RefreshXpDots();
            UpdateLevelUpButton();
        }

        private void OnDestroy()
        {
            _hpSub?.Dispose();
            _foodSub?.Dispose();
            _xpSub?.Dispose();

            if (_levelUpButton != null)
                _levelUpButton.onClick.RemoveListener(OnLevelUpClicked);
        }

        private void OnHpChanged(HpChangedEvent _)     => RefreshHearts();
        private void OnFoodChanged(FoodChangedEvent _) => RefreshFood();

        private void OnXpChanged(XpChangedEvent _)
        {
            RefreshXpDots();
            UpdateLevelUpButton();
        }

        private void OnLevelUpClicked() => _grid?.TryLevelUp();

        private void RefreshHearts()
        {
            if (_grid != null)
                SyncPoolSize(_heartInstances, _heartPrefab, _heartsContainer, _grid.MaxHp);
            ApplyStack(_heartInstances, _context?.Lives ?? -1);
        }

        private void RefreshFood()
        {
            if (_grid != null)
                SyncPoolSize(_foodInstances, _foodPrefab, _foodContainer, _grid.MaxFood);
            ApplyStack(_foodInstances, _context?.Food ?? -1);
        }

        private void RefreshXpDots()
        {
            const int dotCount = hp55games.MapGame.Features.Gameplay.HexGrid.HexGridController.XpPerLevel - 1; // 9
            SyncPoolSize(_xpDotInstances, _xpDotPrefab, _xpDotsContainer, dotCount);

            int xp = _context?.Xp ?? -1;
            bool full = xp >= hp55games.MapGame.Features.Gameplay.HexGrid.HexGridController.XpPerLevel;
            ApplyStack(_xpDotInstances, full ? 0 : xp);
        }

        /// <summary>
        /// Interactable solo quando l'XP è pieno. Usa la stessa costante che TryLevelUp()
        /// usa per decidere se accettare il click — nessun numero duplicato.
        /// </summary>
        private void UpdateLevelUpButton()
        {
            if (_levelUpButton == null || _context == null) return;
            _levelUpButton.interactable =
                _context.Xp >= hp55games.MapGame.Features.Gameplay.HexGrid.HexGridController.XpPerLevel;
        }

        /// <summary>
        /// Porta pool.Count a targetCount, istanziando da prefab dentro container se ne
        /// mancano, distruggendo le eccedenti se ce ne sono di più (es. dopo un
        /// ResetRun a un MaxHp più basso di quello raggiunto nella run precedente).
        /// </summary>
        private static void SyncPoolSize(List<GameObject> pool, GameObject prefab, Transform container, int targetCount)
        {
            if (prefab == null || container == null) return;

            while (pool.Count < targetCount)
                pool.Add(Instantiate(prefab, container));

            while (pool.Count > targetCount)
            {
                int last = pool.Count - 1;
                if (pool[last] != null) Destroy(pool[last]);
                pool.RemoveAt(last);
            }
        }

        /// <summary>
        /// Accende stack[0..count-1], spegne il resto. count viene clampato tra 0 e
        /// stack.Count, così un valore negativo (valori non ancora inizializzati, sentinel
        /// -1) spegne semplicemente tutta la fila senza errori.
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