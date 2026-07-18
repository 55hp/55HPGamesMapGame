using System;
using hp55games.MapGame.Features.Gameplay.HexGrid;
using hp55games.Mobile.Core.Architecture;
using hp55games.Mobile.Core.Context;
using hp55games.Mobile.Core.Gameplay.Events;
using UnityEngine;

namespace hp55games.MapGame.Features.UI
{
    /// <summary>
    /// HUD di debug: mostra ogni parametro di run come testo grezzo invece che come icone.
    /// Pensato per il playtest e il bilanciamento, non per la build finale.
    ///
    /// Volutamente in OnGUI e non su Canvas/TMP: nessun prefab da costruire, nessun campo da
    /// assegnare in Inspector, nessuna dipendenza da TextMeshPro (Game.Features non
    /// referenzia l'assembly TMP). Basta aggiungere questo componente a un GameObject
    /// qualsiasi della scena di gameplay e i valori compaiono a schermo.
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
    /// </summary>
    public sealed class MapGameGameplayHud_Test : MonoBehaviour
    {
        [Header("Posizione e aspetto")]
        [SerializeField] private Vector2 _screenOffset = new Vector2(10f, 10f);
        [SerializeField] private int _fontSize = 22;
        [SerializeField] private bool _showEventCounters = true;

        [Header("Level up")]
        [Tooltip("Mostra un bottone di debug che chiama HexGridController.TryLevelUp().")]
        [SerializeField] private bool _showLevelUpButton = true;
        [Tooltip("Opzionale: se vuoto, viene cercato in scena con FindObjectOfType.")]
        [SerializeField] private HexGridController _grid;

        private IGameContextService _context;
        private IEventBus _bus;

        private IDisposable _hpSub;
        private IDisposable _foodSub;
        private IDisposable _xpSub;
        private IDisposable _scoreSub;
        private IDisposable _deathSub;

        private int _hpEvents;
        private int _foodEvents;
        private int _xpEvents;
        private int _scoreEvents;
        private int _deathEvents;

        private string _lastLevelUpResult = "-";
        private GUIStyle _style;

        private void Awake()
        {
            ServiceRegistry.TryResolve(out _context);
            ServiceRegistry.TryResolve(out _bus);

            if (_grid == null)
                _grid = FindObjectOfType<HexGridController>();

            if (_bus != null)
            {
                _hpSub    = _bus.Subscribe<HpChangedEvent>(_    => _hpEvents++);
                _foodSub  = _bus.Subscribe<FoodChangedEvent>(_  => _foodEvents++);
                _xpSub    = _bus.Subscribe<XpChangedEvent>(_    => _xpEvents++);
                _scoreSub = _bus.Subscribe<ScoreChangedEvent>(_ => _scoreEvents++);
                _deathSub = _bus.Subscribe<PlayerDeathEvent>(_  => _deathEvents++);
            }
        }

        private void OnDestroy()
        {
            _hpSub?.Dispose();
            _foodSub?.Dispose();
            _xpSub?.Dispose();
            _scoreSub?.Dispose();
            _deathSub?.Dispose();
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

            GUILayout.Label("<b>[HUD TEST]</b>", _style);
            GUILayout.Label($"HP      : {_context.Lives}", _style);
            GUILayout.Label($"Cibo    : {_context.Food}", _style);
            GUILayout.Label($"XP      : {_context.Xp} / {HexGridController.XpPerLevel}", _style);
            GUILayout.Label($"Livello : {_context.Level}", _style);
            GUILayout.Label($"Monete  : {_context.Score}", _style);
            GUILayout.Label($"Seed    : {_context.CurrentRunSeed}", _style);

            if (_showEventCounters)
            {
                GUILayout.Space(6f);
                GUILayout.Label("<b>Eventi pubblicati</b>", _style);
                GUILayout.Label($"Hp {_hpEvents}  Food {_foodEvents}  Xp {_xpEvents}  Score {_scoreEvents}  Death {_deathEvents}", _style);
            }

            if (_showLevelUpButton)
            {
                GUILayout.Space(6f);

                if (_grid == null)
                {
                    GUILayout.Label("HexGridController non trovato: level up non disponibile.", _style);
                }
                else
                {
                    bool ready = _context.Xp >= HexGridController.XpPerLevel;
                    GUI.enabled = ready;

                    string label = ready
                        ? "LEVEL UP"
                        : $"LEVEL UP (servono {HexGridController.XpPerLevel - _context.Xp} XP)";

                    if (GUILayout.Button(label, GUILayout.Height(_fontSize * 2f), GUILayout.Width(320f)))
                        _lastLevelUpResult = _grid.TryLevelUp() ? "riuscito" : "rifiutato";

                    GUI.enabled = true;
                    GUILayout.Label($"Ultimo tentativo: {_lastLevelUpResult}", _style);
                }
            }

            GUILayout.EndArea();
        }
    }
}