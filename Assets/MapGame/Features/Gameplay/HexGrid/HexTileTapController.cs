using hp55games.MapGame.Features.Gameplay.CameraControl;
using hp55games.Mobile.Core.Architecture;
using hp55games.Mobile.Core.InputSystem;
using UnityEngine;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Collega il Tap di IInputService (Core) al reveal delle tile.
    /// Pipeline: screen point -> world point (via camera) -> cella Grid -> HexCoord assiale
    /// -> HexGridController.TryRevealTile. Nessuna logica di gameplay qui, solo traduzione
    /// input -> chiamata al controller.
    ///
    /// Doppio tap su tile Scoperta: centra la camera sulla tile senza modificarne la size.
    /// Due tap consecutivi sulla stessa cella entro DoubleTapWindow secondi = doppio tap.
    /// </summary>
    public sealed class HexTileTapController : MonoBehaviour
    {
        [SerializeField] private HexGridController _grid;
        [SerializeField] private Grid _unityGrid;
        [SerializeField] private Camera _camera;

        [Header("Doppio tap")]
        [SerializeField] private MapCameraController _cameraController;
        [Tooltip("Finestra temporale (secondi) entro cui due tap sulla stessa cella contano come doppio tap.")]
        [SerializeField] private float _doubleTapWindow = 0.4f;

        private IInputService _input;
        private HexCoord _lastTapCoord;
        private float    _lastTapTime = float.MinValue;

        private void OnEnable()
        {
            if (_grid == null || _unityGrid == null || _camera == null)
            {
                Debug.LogError("[HexTileTapController] _grid, _unityGrid o _camera non assegnati.");
                return;
            }

            if (ServiceRegistry.TryResolve(out _input))
                _input.Tap += OnTap;
            else
                Debug.LogError("[HexTileTapController] IInputService non registrato in ServiceRegistry.");
        }

        private void OnDisable()
        {
            if (_input != null)
                _input.Tap -= OnTap;
        }

        private void OnTap(Vector2 screenPos)
        {
            // Camera ortografica: qualunque distanza positiva dal piano delle tile (z=0)
            // dà la stessa X/Y risultante, quindi usiamo la distanza reale della camera.
            float distanceToPlane = Mathf.Abs(_camera.transform.position.z);
            Vector3 worldPos = _camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, distanceToPlane));

            Vector3Int cell = _unityGrid.WorldToCell(worldPos);
            var coord = HexCoord.FromOffsetOddR(cell.x, cell.y);

            float now = Time.unscaledTime;
            bool isDoubleTap = coord.Equals(_lastTapCoord) && (now - _lastTapTime) <= _doubleTapWindow;

            _lastTapCoord = coord;
            _lastTapTime  = now;

            if (isDoubleTap)
            {
                HandleDoubleTap(coord, worldPos);
                // Reset per evitare che un terzo tap venga letto come nuovo doppio tap
                _lastTapTime = float.MinValue;
                return;
            }

            bool revealed = _grid.TryRevealTile(coord);
            if (!revealed)
                Debug.Log($"[HexTileTapController] Tap su {coord} ignorato (non cliccabile o coordinata fuori griglia).");
        }

        private void HandleDoubleTap(HexCoord coord, Vector3 worldPos)
        {
            if (_cameraController == null) return;

            if (!_grid.Tiles.TryGetValue(coord, out var tile)) return;

            if (tile.State != TileState.Scoperta)
            {
                Debug.Log($"[HexTileTapController] Doppio tap su {coord} ignorato (tile non Scoperta).");
                return;
            }

            _cameraController.CenterOn(worldPos);
            Debug.Log($"[HexTileTapController] Doppio tap su {coord}: camera centrata.");
        }
    }
}