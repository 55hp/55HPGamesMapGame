using System.Collections;
using System.Collections.Generic;
using hp55games.MapGame.Rules;
using UnityEngine;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Avatar del giocatore sull'ultima tile toccata (solo presentazione, nessuna regola: R-002 resta il clic libero,
    /// salvo la modalità Esplorazione). Alla rivelazione: l'icona del contenuto sfuma, poi l'avatar compare al suo posto.
    /// Parte sulla tile di partenza, di cui nasconde l'icona.
    /// </summary>
    public sealed class PlayerAvatarView : MonoBehaviour
    {
        [SerializeField] private HexGridController _grid;
        [SerializeField] private HexGridViewSpawner _spawner;
        [SerializeField] private SpriteRenderer _avatar;
        [SerializeField] private Vector3 _offset = Vector3.zero;
        [SerializeField] private float _iconFadeOut = 0.25f;
        [SerializeField] private float _avatarFadeIn = 0.25f;

        private Coroutine _move;
        private HexTileView _pendingSuppress;

        private void Awake()
        {
            if (_grid == null || _spawner == null || _avatar == null)
            {
                Debug.LogError("[PlayerAvatarView] _grid, _spawner o _avatar non assegnati.", this);
                enabled = false;
                return;
            }
            SetAlpha(0f);
        }

        private void OnEnable()
        {
            if (_grid == null) return;
            _grid.RuleEventsRaised += OnEvents;
        }

        private void OnDisable()
        {
            if (_grid != null) _grid.RuleEventsRaised -= OnEvents;
        }

        private void OnEvents(IReadOnlyList<RuleEvent> events)
        {
            if (!enabled) return;
            int last = -1;
            bool start = false;
            foreach (var e in events)
            {
                if (e.Type != RuleEventType.TileRevealed) continue;
                last = e.TileIndex;
                start = e.Kind == ContentKind.Start;
            }
            if (last < 0) return;
            var coord = _grid.CoordOf(last);
            _spawner.Views.TryGetValue(coord, out var view);
            if (_move != null) { StopCoroutine(_move); FinishPending(); }
            _move = StartCoroutine(start ? PlaceInstant(coord, view) : MoveTo(coord, view));
        }

        private IEnumerator PlaceInstant(HexCoord coord, HexTileView view)
        {
            view?.SuppressIcon();
            transform.position = _spawner.GetWorldPosition(coord) + _offset;
            SetAlpha(1f);
            yield break;
        }

        private IEnumerator MoveTo(HexCoord coord, HexTileView view)
        {
            _pendingSuppress = view;
            float t = 0f;
            while (t < _iconFadeOut)
            {
                t += Time.deltaTime;
                float a = 1f - Mathf.Clamp01(t / _iconFadeOut);
                view?.SetIconAlpha(a);
                SetAlpha(a); // l'avatar lascia la tile precedente insieme
                yield return null;
            }
            FinishPending();
            transform.position = _spawner.GetWorldPosition(coord) + _offset;
            t = 0f;
            while (t < _avatarFadeIn)
            {
                t += Time.deltaTime;
                SetAlpha(Mathf.Clamp01(t / _avatarFadeIn));
                yield return null;
            }
            SetAlpha(1f);
            _move = null;
        }

        private void FinishPending()
        {
            if (_pendingSuppress == null) return;
            _pendingSuppress.SuppressIcon();
            _pendingSuppress.SetIconAlpha(1f);
            _pendingSuppress = null;
        }

        private void SetAlpha(float a)
        {
            var c = _avatar.color; c.a = a; _avatar.color = c;
        }
    }
}
