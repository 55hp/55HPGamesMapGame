using hp55games.Mobile.Core.Architecture;
using hp55games.Mobile.Core.InputSystem;
using UnityEngine;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Collega il Tap di IInputService (Core) al reveal delle tile.
    /// Pipeline: screen point -> world point (via camera) -> cella Grid -> HexCoord assiale
    /// -> HexGridController.TryRevealTile. Nessuna LogErrorica di gameplay qui, solo traduzione
    /// input -> chiamata al controller.
    /// </summary>
    public sealed class HexTileTapController : MonoBehaviour
    {
        [SerializeField] private HexGridController _grid;
        [SerializeField] private Grid _unityGrid;
        [SerializeField] private Camera _camera;

        private IInputService _input;

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
            var coord = HexCoord.FromOffsetOddQ(cell.y, cell.x);

            bool revealed = _grid.TryRevealTile(coord);
            if (!revealed)
                Debug.Log($"[HexTileTapController] Tap su {coord} ignorato (non cliccabile o coordinata fuori griglia).");
        }
    }
}
