using System.Collections.Generic;
using hp55games.MapGame.Features.Gameplay.HexGrid;
using UnityEngine;

namespace hp55games.MapGame.Features.Gameplay.CameraControl
{
    /// <summary>
    /// Gestisce il movimento smooth della camera di gioco sulla griglia esagonale.
    /// Si auto-iscrive a HexGridController.TileRevealed e, a ogni rivelazione:
    ///   1. Centra la camera sulla tile appena scoperta (smooth position).
    ///   2. Ridimensiona l'orthoSize per inquadrare tutte le tile Scoperte + offset (smooth size).
    ///
    /// FocusOn() è esposto pubblicamente per impulsi esterni (debug, cutscene, ecc.).
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
    public sealed class MapCameraController : MonoBehaviour
    {
        [Header("Riferimenti")]
        [SerializeField] private HexGridController _grid;
        [SerializeField] private HexGridViewSpawner _spawner;

        [Header("Framing")]
        [Tooltip("Padding ortho-space aggiunto intorno al bounding box delle tile Scoperte.")]
        [SerializeField] private float _scopertaOffset = 1.2f;
        [Tooltip("OrthoSize minimo garantito anche con una sola tile scoperta.")]
        [SerializeField] private float _minOrthoSize = 2f;

        [Header("Smoothing")]
        [SerializeField] private float _positionSmoothTime = 0.35f;
        [SerializeField] private float _sizeSmoothTime = 0.45f;

        private UnityEngine.Camera _camera;
        private Vector3 _targetPosition;
        private float _targetSize;
        private Vector3 _positionVelocity;
        private float _sizeVelocity;

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            _camera = GetComponent<UnityEngine.Camera>();

            if (!_camera.orthographic)
                Debug.LogWarning("[MapCameraController] La camera non è ortografica: orthographicSize non avrà effetto.");

            _targetPosition = _camera.transform.position;
            _targetSize = _camera.orthographicSize;
        }

        private void OnEnable()
        {
            if (_grid != null)
                _grid.TileRevealed += OnTileRevealed;
        }

        private void OnDisable()
        {
            if (_grid != null)
                _grid.TileRevealed -= OnTileRevealed;
        }

        private void Update()
        {
            var t = _camera.transform;
            t.position = Vector3.SmoothDamp(t.position, _targetPosition, ref _positionVelocity, _positionSmoothTime);
            _camera.orthographicSize = Mathf.SmoothDamp(
                _camera.orthographicSize, _targetSize, ref _sizeVelocity, _sizeSmoothTime);
        }

        // ── API pubblica ───────────────────────────────────────────────────────

        /// <summary>
        /// Centra smooth la camera su <paramref name="worldCenter"/> senza modificare la size corrente.
        /// Usato dal doppio tap su tile Scoperta.
        /// </summary>
        public void CenterOn(Vector3 worldCenter)
        {
            _targetPosition = new Vector3(worldCenter.x, worldCenter.y, _camera.transform.position.z);
        }

        /// <summary>
        /// Impulso esterno: sposta smooth la camera su <paramref name="worldCenter"/>
        /// e imposta il target orthoSize su <paramref name="orthoSize"/>.
        /// </summary>
        public void FocusOn(Vector3 worldCenter, float orthoSize)
        {
            _targetPosition = new Vector3(worldCenter.x, worldCenter.y, _camera.transform.position.z);
            _targetSize = Mathf.Max(orthoSize, _minOrthoSize);
        }

        // ── Logica interna ─────────────────────────────────────────────────────

        private void OnTileRevealed(HexTileData revealed, IReadOnlyList<HexTileData> _neighbors)
        {
            if (_spawner == null || _grid == null) return;

            // AABB composita di tutte le tile Scoperte (include la tile appena rivelata)
            ComputeScopertaBounds(out Vector3 bMin, out Vector3 bMax);

            // 1. Posizione target = centroide dell'area composita
            Vector3 center = (bMin + bMax) * 0.5f;
            _targetPosition = new Vector3(center.x, center.y, _camera.transform.position.z);

            // 2. OrthoSize = metà della dimensione maggiore dell'AABB + offset
            float width  = bMax.x - bMin.x;
            float height = bMax.y - bMin.y;
            float sizeForHeight = height * 0.5f + _scopertaOffset;
            float sizeForWidth  = (width  * 0.5f + _scopertaOffset) / _camera.aspect;
            _targetSize = Mathf.Max(sizeForHeight, sizeForWidth, _minOrthoSize);
        }

        /// <summary>
        /// Calcola AABB (XY) delle tile nello stato Scoperta usando GetWorldPosition dello spawner.
        /// </summary>
        private void ComputeScopertaBounds(out Vector3 min, out Vector3 max)
        {
            min = new Vector3(float.MaxValue,  float.MaxValue,  0f);
            max = new Vector3(float.MinValue, float.MinValue, 0f);

            foreach (var (coord, data) in _grid.Tiles)
            {
                if (data.State != TileState.Scoperta) continue;

                Vector3 p = _spawner.GetWorldPosition(coord);
                if (p.x < min.x) min.x = p.x;
                if (p.y < min.y) min.y = p.y;
                if (p.x > max.x) max.x = p.x;
                if (p.y > max.y) max.y = p.y;
            }

            // Fallback: nessuna tile Scoperta trovata
            if (min.x == float.MaxValue)
                min = max = Vector3.zero;
        }
    }
}
