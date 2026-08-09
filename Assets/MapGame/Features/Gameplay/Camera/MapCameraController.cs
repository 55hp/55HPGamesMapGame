using hp55games.MapGame.Features.Gameplay.HexGrid;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace hp55games.MapGame.Features.Gameplay.CameraControl
{
    /// <summary>
    /// Gestisce il movimento smooth della camera di gioco sulla griglia esagonale.
    ///
    /// Inquadratura iniziale: al primo GridInitialized della run (sia al vero avvio sia
    /// dopo un retry, la griglia si rigenera sempre da capo) inquadra TUTTA la plancia,
    /// non solo le tile Scoperte, con un offset k = larghezza orizzontale massima di una
    /// tile (HexGridViewSpawner.CellSize.x) x 2. Applicata a scatto, non smooth: non ha
    /// senso vedere la camera animarsi dalla posizione di default dell'Inspector fino
    /// all'inquadratura calcolata, deve essere gia' corretta al primo frame visibile.
    ///
    /// Dopo l'inquadratura iniziale la camera non si sposta piu' automaticamente: ne'
    /// posizione ne' zoom cambiano al reveal di una tile. Da li' in poi il giocatore
    /// controlla la camera manualmente — drag (ApplyDragPan), zoom da tastiera I/O
    /// (ApplyKeyboardZoom) o rotella mouse (ApplyMouseWheelZoom).
    ///
    /// FocusOn() è esposto pubblicamente per impulsi esterni (debug, cutscene, ecc.).
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
    public sealed class MapCameraController : MonoBehaviour
    {
        [Header("Riferimenti")]
        [FormerlySerializedAs("_grid")]
        [SerializeField] private HexGridController _hexGrid;
        [FormerlySerializedAs("_spawner")]
        [SerializeField] private HexGridViewSpawner _tileViewSpawner;

        [Header("Framing")]
        [FormerlySerializedAs("_minOrthoSize")]
        [Tooltip("OrthoSize minimo garantito anche con una sola tile scoperta.")]
        [SerializeField] private float _minOrthographicSize = 2f;

        [Header("Smoothing")]
        [FormerlySerializedAs("_positionSmoothTime")]
        [SerializeField] private float _positionSmoothTime = 0.35f;
        [FormerlySerializedAs("_sizeSmoothTime")]
        [SerializeField] private float _orthographicSizeSmoothTime = 0.45f;

        [Header("Zoom (debug/desktop — I/O tastiera, rotella mouse)")]
        [SerializeField] private float _keyboardZoomSpeed = 20f;
        [SerializeField] private float _keyboardZoomMinSize = 5f;
        [SerializeField] private float _keyboardZoomMaxSize = 50f;
        [Tooltip("OrthoSize applicato per unita' di Input.mouseScrollDelta.y — stesso range [_keyboardZoomMinSize, _keyboardZoomMaxSize] della tastiera.")]
        [SerializeField] private float _mouseWheelZoomSpeed = 5f;

        private UnityEngine.Camera _camera;
        private Vector3 _targetPosition;
        private float _targetSize;
        private Vector3 _positionVelocity;
        private float _sizeVelocity;

        // Drag-to-pan: mouse-click drag su PC/Play Mode, touch drag su mobile — stessa
        // logica, il branch touch/mouse e' l'unica differenza (vedi ApplyDragPan).
        private bool _isDragging;
        private int _dragTouchId = -1; // -1 = mouse
        private Vector2 _dragLastScreenPos;

        // ── Unity lifecycle ────────────────────────────────────────────────────

        private void Awake()
        {
            _camera = GetComponent<UnityEngine.Camera>();

            if (!_camera.orthographic)
                Debug.LogWarning("[MapCameraController] La camera non è ortografica: orthographicSize non avrà effetto.");

            // Placeholder finché non arriva il primo GridInitialized (vedi OnGridInitialized):
            // qui non possiamo ancora calcolare l'inquadratura reale, la griglia potrebbe non
            // esistere ancora (BuildGrid è chiamato da HexGridController fuori da Awake,
            // vedi HexGridController.InitializeSession — stesso motivo, evitare corse tra
            // Awake/OnEnable di componenti diversi).
            _targetPosition = _camera.transform.position;
            _targetSize = _camera.orthographicSize;
        }

        private void OnEnable()
        {
            if (_hexGrid != null)
                _hexGrid.GridInitialized += OnGridInitialized;
        }

        private void OnDisable()
        {
            if (_hexGrid != null)
                _hexGrid.GridInitialized -= OnGridInitialized;
        }

        private void Update()
        {
            ApplyKeyboardZoom();
            ApplyMouseWheelZoom();
            ApplyDragPan();

            var t = _camera.transform;
            t.position = Vector3.SmoothDamp(t.position, _targetPosition, ref _positionVelocity, _positionSmoothTime);
            _camera.orthographicSize = Mathf.SmoothDamp(
                _camera.orthographicSize, _targetSize, ref _sizeVelocity, _orthographicSizeSmoothTime);
        }

        /// <summary>
        /// Drag-to-pan: click+trascina col mouse (PC/Play Mode) o trascina col dito
        /// (mobile) per spostare la camera. Stesso pattern single-pointer di InputService
        /// (touch se supportato e presente, altrimenti mouse) ma non passa da
        /// IInputService: quel servizio classifica il gesto solo a rilascio (Tap/Swipe),
        /// qui serve il delta continuo frame-per-frame mentre il pointer resta premuto.
        /// HexTileTapController ascolta solo Tap, non Swipe, quindi un drag esteso non
        /// tocca mai la logica di reveal — nessun conflitto tra i due sistemi.
        ///
        /// Applica il delta direttamente a _camera.transform.position (non smooth: un
        /// drag deve seguire il dito 1:1, non con il lag di SmoothDamp) e tiene
        /// _targetPosition/_positionVelocity allineati cosi' Update() non fa scattare la
        /// camera indietro subito dopo il rilascio.
        /// </summary>
        private void ApplyDragPan()
        {
            bool isPressed;
            Vector2 currentScreenPos;
            int touchId = -1;

            if (Input.touchSupported && Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                currentScreenPos = touch.position;
                touchId = touch.fingerId;
                isPressed = touch.phase == TouchPhase.Began ||
                            touch.phase == TouchPhase.Moved ||
                            touch.phase == TouchPhase.Stationary;
            }
            else
            {
                isPressed = Input.GetMouseButton(0);
                currentScreenPos = Input.mousePosition;
            }

            if (!isPressed)
            {
                _isDragging = false;
                _dragTouchId = -1;
                return;
            }

            if (!_isDragging)
            {
                // Deciso solo all'inizio del press, come InputService: un drag che parte
                // su un bottone/popup non deve spostare la camera sotto.
                if (IsPointerOverUI(touchId)) return;

                _isDragging = true;
                _dragTouchId = touchId;
                _dragLastScreenPos = currentScreenPos;
                return; // primo frame: solo l'ancora, nessun delta ancora da applicare
            }

            if (touchId != _dragTouchId) return; // un altro dito e' subentrato: ignora finche' non si rilascia

            // Camera ortografica: qualunque distanza positiva dal piano delle tile (z=0)
            // da' lo stesso X/Y risultante — stessa tecnica di HexTileTapController.OnTap.
            float depth = Mathf.Abs(_camera.transform.position.z);
            Vector3 worldLast    = _camera.ScreenToWorldPoint(new Vector3(_dragLastScreenPos.x, _dragLastScreenPos.y, depth));
            Vector3 worldCurrent = _camera.ScreenToWorldPoint(new Vector3(currentScreenPos.x, currentScreenPos.y, depth));
            Vector3 worldDelta   = worldCurrent - worldLast;

            if (worldDelta != Vector3.zero)
            {
                Vector3 newPosition = _camera.transform.position - worldDelta;
                _camera.transform.position = newPosition;

                // Allineati subito: evita che il prossimo SmoothDamp in Update() interpreti
                // lo spostamento come uno scarto da recuperare.
                _targetPosition = newPosition;
                _positionVelocity = Vector3.zero;
            }

            _dragLastScreenPos = currentScreenPos;
        }

        /// <summary>True se il press che sta iniziando ora e' sopra un elemento UI — stesso pattern di InputService.IsPointerOverUI.</summary>
        private static bool IsPointerOverUI(int touchId)
        {
            if (EventSystem.current == null) return false;

            return touchId >= 0
                ? EventSystem.current.IsPointerOverGameObject(touchId)
                : EventSystem.current.IsPointerOverGameObject();
        }

        /// <summary>
        /// Zoom da tastiera (debug/desktop): "I" avvicina (orthoSize minore), "O"
        /// allontana (orthoSize maggiore), clampato a [_keyboardZoomMinSize,
        /// _keyboardZoomMaxSize]. Modifica _targetSize, non orthographicSize
        /// direttamente: passa dallo stesso SmoothDamp di Update, coerente con
        /// FocusOn/OnGridInitialized.
        /// </summary>
        private void ApplyKeyboardZoom()
        {
            float zoomDelta = 0f;
            if (Input.GetKey(KeyCode.I)) zoomDelta -= _keyboardZoomSpeed * Time.unscaledDeltaTime;
            if (Input.GetKey(KeyCode.O)) zoomDelta += _keyboardZoomSpeed * Time.unscaledDeltaTime;

            if (zoomDelta == 0f) return;

            _targetSize = Mathf.Clamp(_targetSize + zoomDelta, _keyboardZoomMinSize, _keyboardZoomMaxSize);
        }

        /// <summary>
        /// Zoom da rotella mouse: scroll avanti (valore positivo) avvicina (orthoSize
        /// minore), scroll indietro allontana — stesso clamp e stesso _targetSize di
        /// ApplyKeyboardZoom, cosi' i due input restano intercambiabili senza superare
        /// [_keyboardZoomMinSize, _keyboardZoomMaxSize].
        /// </summary>
        private void ApplyMouseWheelZoom()
        {
            float scroll = Input.mouseScrollDelta.y;
            if (scroll == 0f) return;

            _targetSize = Mathf.Clamp(_targetSize - scroll * _mouseWheelZoomSpeed, _keyboardZoomMinSize, _keyboardZoomMaxSize);
        }

        // ── API pubblica ───────────────────────────────────────────────────────

        /// <summary>
        /// Impulso esterno: sposta smooth la camera su <paramref name="worldCenter"/>
        /// e imposta il target orthoSize su <paramref name="orthoSize"/>.
        /// </summary>
        public void FocusOn(Vector3 worldCenter, float orthoSize)
        {
            _targetPosition = new Vector3(worldCenter.x, worldCenter.y, _camera.transform.position.z);
            _targetSize = Mathf.Max(orthoSize, _minOrthographicSize);
        }

        // ── Logica interna ─────────────────────────────────────────────────────

        /// <summary>
        /// Primo momento della run (o dopo un retry, la griglia si rigenera sempre da
        /// capo): inquadra tutta la plancia con offset k = larghezza orizzontale massima
        /// di una tile x 2. A scatto, non smooth — vedi doc di classe.
        /// </summary>
        private void OnGridInitialized()
        {
            if (_tileViewSpawner == null || _hexGrid == null) return;

            ComputeFullBoardBounds(out Vector3 bMin, out Vector3 bMax);

            Vector3 center = (bMin + bMax) * 0.5f;
            float width  = bMax.x - bMin.x;
            float height = bMax.y - bMin.y;

            float tileWidth = _tileViewSpawner.CellSize.x;
            float k = tileWidth * 2f;

            float sizeForHeight = height * 0.5f + k;
            float sizeForWidth  = (width  * 0.5f + k) / _camera.aspect;

            _targetPosition = new Vector3(center.x, center.y, _camera.transform.position.z);
            _targetSize = Mathf.Max(sizeForHeight, sizeForWidth, _minOrthographicSize);

            // A scatto: niente animazione dalla posizione Inspector-default a questa.
            _camera.transform.position = _targetPosition;
            _camera.orthographicSize = _targetSize;
            _positionVelocity = Vector3.zero;
            _sizeVelocity = 0f;
        }

        /// <summary>
        /// Calcola AABB (XY) di TUTTE le tile della griglia, Scoperte o no — usato solo
        /// per l'inquadratura iniziale (vedi OnGridInitialized).
        /// </summary>
        private void ComputeFullBoardBounds(out Vector3 min, out Vector3 max)
        {
            min = new Vector3(float.MaxValue,  float.MaxValue,  0f);
            max = new Vector3(float.MinValue, float.MinValue, 0f);

            foreach (var (coord, _) in _hexGrid.Tiles)
            {
                Vector3 p = _tileViewSpawner.GetWorldPosition(coord);
                if (p.x < min.x) min.x = p.x;
                if (p.y < min.y) min.y = p.y;
                if (p.x > max.x) max.x = p.x;
                if (p.y > max.y) max.y = p.y;
            }

            if (min.x == float.MaxValue)
                min = max = Vector3.zero;
        }
    }
}
