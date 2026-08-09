using System;
using hp55games.Mobile.Core.Architecture;
using hp55games.Mobile.Core.Context;
using hp55games.Mobile.Core.Gameplay.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace hp55games.MapGame.Features.UI
{
    /// <summary>
    /// HUD specifico di MapGame. Mostra tre coppie (icona, contatore) per HP, Cibo e Monete,
    /// più N icone on/off per i collezionabili unici (Chiavi DL4/5/6).
    ///
    /// Ogni coppia ha un campo Image per l'icona (sprite assegnato in Inspector, invariato a
    /// runtime) e un TextMeshProUGUI per il valore numerico aggiornato via evento bus.
    /// Le icone chiave vengono mostrate/nascoste (SetActive) alla ricezione di KeysChangedEvent.
    /// </summary>
    public sealed class MapGameGameplayHud : MonoBehaviour
    {
        [Header("HP")]
        [SerializeField] private Image           _hpIcon;
        [SerializeField] private TextMeshProUGUI _hpLabel;

        [Header("Food")]
        [SerializeField] private Image           _foodIcon;
        [SerializeField] private TextMeshProUGUI _foodLabel;

        [Header("Coins")]
        [SerializeField] private Image           _coinsIcon;
        [SerializeField] private TextMeshProUGUI _coinsLabel;

        [Header("Chiavi (collezionabili unici)")]
        [Tooltip("Icona visibile solo quando HasKeyDL4 = true.")]
        [SerializeField] private GameObject _keyDL4Icon;
        [Tooltip("Icona visibile solo quando HasKeyDL5 = true.")]
        [SerializeField] private GameObject _keyDL5Icon;
        [Tooltip("Icona visibile solo quando HasKeyDL6 = true.")]
        [SerializeField] private GameObject _keyDL6Icon;

        private IGameContextService _context;
        private IEventBus           _bus;

        private IDisposable _hpSub;
        private IDisposable _foodSub;
        private IDisposable _coinsSub;
        private IDisposable _keysSub;

        private void Awake()
        {
            ServiceRegistry.TryResolve(out _context);
            ServiceRegistry.TryResolve(out _bus);

            if (_bus != null)
            {
                _hpSub    = _bus.Subscribe<HpChangedEvent>(OnHpChanged);
                _foodSub  = _bus.Subscribe<FoodChangedEvent>(OnFoodChanged);
                _coinsSub = _bus.Subscribe<ScoreChangedEvent>(OnCoinsChanged);
                _keysSub  = _bus.Subscribe<KeysChangedEvent>(OnKeysChanged);
            }
        }

        private void Start() => Refresh();

        private void OnDestroy()
        {
            _hpSub?.Dispose();
            _foodSub?.Dispose();
            _coinsSub?.Dispose();
            _keysSub?.Dispose();
        }

        // ── Handlers ──────────────────────────────────────────────────────────────

        private void OnHpChanged(HpChangedEvent _)       => RefreshHp();
        private void OnFoodChanged(FoodChangedEvent _)   => RefreshFood();
        private void OnCoinsChanged(ScoreChangedEvent _) => RefreshCoins();
        private void OnKeysChanged(KeysChangedEvent _)   => RefreshKeys();

        // ── Refresh ───────────────────────────────────────────────────────────────

        private void Refresh()
        {
            RefreshHp();
            RefreshFood();
            RefreshCoins();
            RefreshKeys();
        }

        private void RefreshHp()
        {
            if (_hpLabel != null)
                _hpLabel.text = Mathf.Max(0, _context?.Lives ?? 0).ToString();
        }

        private void RefreshFood()
        {
            if (_foodLabel != null)
                _foodLabel.text = Mathf.Max(0, _context?.Food ?? 0).ToString();
        }

        private void RefreshCoins()
        {
            if (_coinsLabel != null)
                _coinsLabel.text = Mathf.Max(0, _context?.Score ?? 0).ToString();
        }

        private void RefreshKeys()
        {
            if (_keyDL4Icon != null) _keyDL4Icon.SetActive(_context?.HasKeyDL4 ?? false);
            if (_keyDL5Icon != null) _keyDL5Icon.SetActive(_context?.HasKeyDL5 ?? false);
            if (_keyDL6Icon != null) _keyDL6Icon.SetActive(_context?.HasKeyDL6 ?? false);
        }
    }
}
