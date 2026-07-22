using System;
using System.Threading.Tasks;
using hp55games.MapGame.Features.Gameplay.HexGrid;
using hp55games.Mobile.Core;
using hp55games.Mobile.Core.Architecture;
using hp55games.Mobile.Core.Context;
using hp55games.Mobile.Core.Gameplay.Events;
using hp55games.Mobile.Core.UI;
using UnityEngine;

namespace hp55games.MapGame.Features.UI
{
    /// <summary>
    /// HUD di debug: mostra ogni parametro di run come testo grezzo invece che come icone.
    /// Pensato per il playtest e il bilanciamento, non per la build finale.
    ///
    /// Volutamente in OnGUI e non su Canvas/TMP: nessun prefab da costruire, nessun campo da
    /// assegnare in Inspector. Basta aggiungere questo componente a un GameObject qualsiasi
    /// della scena di gameplay e i valori compaiono a schermo.
    ///
    /// Puo' convivere con MapGameGameplayHud (icone) — leggono entrambi lo stesso
    /// IGameContextService, non si disturbano. Tienili entrambi attivi per verificare che le
    /// icone rispecchino i numeri reali.
    ///
    /// Legge lo stato in modo passivo a ogni frame (OnGUI), quindi non puo' desincronizzarsi
    /// nemmeno se un evento non venisse pubblicato. Si iscrive comunque agli eventi solo per
    /// contarli: se un contatore non sale quando il valore cambia, il bug e' nella
    /// pubblicazione dell'evento, non nella logica di gioco. E' la ragione principale per cui
    /// questo HUD e' utile in debug.
    ///
    /// 2026-07-20 (Economia + Win Condition): XP/Livello rimossi insieme al bottone level
    /// up. Al loro posto: cap runtime HP/Cibo accanto ai correnti, contatore Victory, e un
    /// bottone SHOP di debug che apre UIPopup_Shop (stesso popup della UI reale, cosi' il
    /// flusso acquisti e' testabile prima che esista un ingresso di gioco allo shop).
    /// </summary>
    public sealed class MapGameGameplayHud_Test : MonoBehaviour
    {
        [Header("Posizione e aspetto")]
        [SerializeField] private Vector2 _screenOffset = new Vector2(10f, 10f);
        [SerializeField] private int _fontSize = 22;
        [SerializeField] private bool _showEventCounters = true;

        [Header("Shop (debug)")]
        [Tooltip("Mostra un bottone di debug che apre il popup dello shop.")]
        [SerializeField] private bool _showShopButton = true;
        [Tooltip("Opzionale: se vuoto, viene cercato in scena con FindObjectOfType.")]
        [SerializeField] private HexGridController _grid;

        private IGameContextService _context;
        private IEventBus _bus;

        private IDisposable _hpSub;
        private IDisposable _foodSub;
        private IDisposable _scoreSub;
        private IDisposable _deathSub;
        private IDisposable _victorySub;

        private int _hpEvents;
        private int _foodEvents;
        private int _scoreEvents;
        private int _deathEvents;
        private int _victoryEvents;

        private bool _shopOpening;
        private GUIStyle _style;

        private void Awake()
        {
            ServiceRegistry.TryResolve(out _context);
            ServiceRegistry.TryResolve(out _bus);

            if (_grid == null)
                _grid = FindObjectOfType<HexGridController>();

            if (_bus != null)
            {
                _hpSub      = _bus.Subscribe<HpChangedEvent>(_      => _hpEvents++);
                _foodSub    = _bus.Subscribe<FoodChangedEvent>(_    => _foodEvents++);
                _scoreSub   = _bus.Subscribe<ScoreChangedEvent>(_   => _scoreEvents++);
                _deathSub   = _bus.Subscribe<PlayerDeathEvent>(_    => _deathEvents++);
                _victorySub = _bus.Subscribe<PlayerVictoryEvent>(_  => _victoryEvents++);
            }
        }

        private void OnDestroy()
        {
            _hpSub?.Dispose();
            _foodSub?.Dispose();
            _scoreSub?.Dispose();
            _deathSub?.Dispose();
            _victorySub?.Dispose();
        }

        private void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    fontSize  = _fontSize,
                    alignment = TextAnchor.UpperLeft,
                    richText  = true,
                };
            }
            _style.fontSize = _fontSize;

            // Larghezza generosa: i valori sono brevi, ma il box deve reggere anche i
            // messaggi di stato quando un servizio non e' risolto.
            var area = new Rect(_screenOffset.x, _screenOffset.y, 520f, Screen.height - _screenOffset.y);
            GUILayout.BeginArea(area);

            if (_context == null)
            {
                GUILayout.Label("<b>[HUD TEST]</b> IGameContextService non risolto.", _style);
                GUILayout.EndArea();
                return;
            }

            string hpCap   = _grid != null ? $" / {_grid.CurrentMaxHp}" : "";
            string foodCap = _grid != null ? $" / {_grid.CurrentMaxFood}" : "";

            GUILayout.Label("<b>[HUD TEST]</b>", _style);
            GUILayout.Label($"HP      : {_context.Lives}{hpCap}", _style);
            GUILayout.Label($"Cibo    : {_context.Food}{foodCap}", _style);
            GUILayout.Label($"Monete  : {_context.Score}", _style);
            GUILayout.Label($"Seed    : {_context.CurrentRunSeed}", _style);

            if (_showEventCounters)
            {
                GUILayout.Space(6f);
                GUILayout.Label("<b>Eventi pubblicati</b>", _style);
                GUILayout.Label($"Hp {_hpEvents}  Food {_foodEvents}  Score {_scoreEvents}  Death {_deathEvents}  Victory {_victoryEvents}", _style);
            }

            if (_showShopButton)
            {
                GUILayout.Space(6f);

                if (_grid == null)
                {
                    GUILayout.Label("HexGridController non trovato: shop non disponibile.", _style);
                }
                else
                {
                    GUI.enabled = !_shopOpening;
                    if (GUILayout.Button("SHOP", GUILayout.Height(_fontSize * 2f), GUILayout.Width(320f)))
                        AsyncUtils.FireAndForget(OpenShopAsync(), context: nameof(MapGameGameplayHud_Test));
                    GUI.enabled = true;
                }
            }

            GUILayout.EndArea();
        }

        private async Task OpenShopAsync()
        {
            // Guard contro doppio click mentre l'istanza Addressable sta caricando.
            _shopOpening = true;
            try
            {
                if (!ServiceRegistry.TryResolve<IUIPopupService>(out var popupService))
                {
                    Debug.LogError("[HUD TEST] IUIPopupService non risolto: shop non apribile.");
                    return;
                }

                var popup = await popupService.OpenAsync<UIPopup_Shop>(Addr.Content.UI.Popups.Popup_Shop);
                if (popup == null)
                {
                    Debug.LogError("[HUD TEST] Impossibile aprire UIPopup_Shop (address non registrato o Addressables non buildate).");
                    return;
                }

                popup.Open(_grid);
            }
            finally
            {
                _shopOpening = false;
            }
        }
    }
}
