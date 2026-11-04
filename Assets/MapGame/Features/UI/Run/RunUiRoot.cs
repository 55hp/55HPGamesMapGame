using System.Collections.Generic;
using hp55games.MapGame.Features.Gameplay.Run;
using hp55games.MapGame.Rules;
using hp55games.Mobile.Core.Architecture;
using UnityEngine;

namespace hp55games.MapGame.Features.UI.Run
{
    /// <summary>
    /// Radice del prefab UIRunHUD (spec 0006). Trova la partita (IRunSession) nel ServiceRegistry e
    /// distribuisce stato ed eventi alle viste figlie. Registrandosi come IRunHud spegne la UI di prototipo.
    /// </summary>
    public sealed class RunUiRoot : MonoBehaviour, IRunHud
    {
        [SerializeField] private RunHudView _hud;
        [SerializeField] private DecisionPanelView _decision;
        [SerializeField] private BoardLineLabelsView _lineLabels;
        [SerializeField] private EndPanelView _end;

        private IRunSession _session;

        private void Awake()
        {
            if (_hud == null || _decision == null || _lineLabels == null || _end == null)
            {
                Debug.LogError("[RunUiRoot] Viste non assegnate (_hud, _decision, _lineLabels, _end).", this);
                enabled = false;
                return;
            }
            ServiceRegistry.Register<IRunHud>(this);
        }

        private void Update()
        {
            if (_session == null || (_session is Object o && o == null)) TryBind();
        }

        private void OnDestroy() => Unbind();

        private void TryBind()
        {
            _session = null;
            if (!ServiceRegistry.TryResolve(out IRunSession s) || s == null || (s is Object o && o == null)) return;
            _session = s;
            _session.RuleEventsRaised += OnEvents;
            _session.SessionStarted += OnSessionStarted;
            _hud.Bind(_session);
            _decision.Bind(_session);
            _lineLabels.Bind(_session);
            _end.Bind(_session);
            OnSessionStarted();
        }

        private void Unbind()
        {
            if (_session == null) return;
            _session.RuleEventsRaised -= OnEvents;
            _session.SessionStarted -= OnSessionStarted;
            _session = null;
        }

        private void OnSessionStarted()
        {
            RefreshAll();
        }

        private void OnEvents(IReadOnlyList<RuleEvent> events)
        {
            var e = _session?.Engine;
            if (e == null) return;
            RefreshAll();
        }

        private void RefreshAll()
        {
            var e = _session?.Engine;
            if (e == null) return;
            _hud.Refresh(e);
            _decision.Refresh(e);
            _lineLabels.Refresh(e);
            _end.Refresh(e);
        }
    }
}
