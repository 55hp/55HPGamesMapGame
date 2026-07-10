using UnityEngine;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Rappresentazione visiva di una tile.
    /// ApplyState gestisce l'aspetto non-risolto (Sconosciuta / Conosciuta).
    /// Reveal mostra l'ambiente scoperto: l'arte ambientale è il sistema di hint.
    /// SetClickable attiva/disattiva l'indicatore di clickability.
    /// Nessuna logica di gameplay qui: solo rendering.
    ///
    /// Gli sprite sono centralizzati in TileVisualConfig (asset condiviso) invece che
    /// assegnati per-istanza qui — un solo posto da aggiornare quando cambia un tipo o
    /// se ne aggiunge uno nuovo, anche con più varianti di prefab in futuro.
    /// </summary>
    public sealed class HexTileView : MonoBehaviour
    {
        [Header("Riferimenti")]
        [SerializeField] private SpriteRenderer _background;
        [SerializeField] private SpriteRenderer _icon;
        [SerializeField] private SpriteRenderer _alpha;

        [Header("Configurazione visiva condivisa")]
        [SerializeField] private TileVisualConfig _visualConfig;

        [Header("Indicatore clickability (child GameObject, wired in prefab)")]
        [SerializeField] private GameObject _clickableIndicator;

        public HexCoord Coord { get; private set; }

        public void Setup(HexCoord coord)
        {
            Coord = coord;
        }

        /// <summary>
        /// Aggiorna lo sprite del background in base allo stato di conoscenza.
        /// Sconosciuta → nuvole (nessuna informazione).
        /// Conosciuta e Scoperta → arte del bioma. Il background NON cambia mai al
        /// reveal: l'ambiente è il sistema di hint e resta lo stesso prima e dopo aver
        /// giocato l'evento.
        /// </summary>
        public void ApplyState(TileState state)
        {
            if (_background == null || _visualConfig == null) return;

            _background.sprite = state switch
            {
                TileState.Sconosciuta => _visualConfig.SconosciutaSprite,
                _                     => _visualConfig.ConosciutaSprite,
            };

            _icon?.gameObject.SetActive(false);
            _alpha?.gameObject.SetActive(false);
        }

        /// <summary>
        /// Attiva o disattiva l'indicatore visivo di clickability.
        /// Null-safe: se _clickableIndicator non è assegnato, non fa nulla.
        /// </summary>
        public void SetClickable(bool clickable)
        {
            _clickableIndicator?.SetActive(clickable);
        }

        /// <summary>
        /// Mostra il record dell'evento risolto: attiva l'icona (tipo specifico, presa
        /// da TileVisualConfig) e l'overlay alpha (marca la tile come parte del set già
        /// rivelato). Non è un hint — arriva solo dopo l'interazione del giocatore.
        /// Background invariato: l'ambiente resta quello del bioma già mostrato da
        /// Conosciuta.
        ///
        /// Strada e Neutra non hanno contenuto evento: l'alpha viene attivato (tile
        /// già percorsa) ma l'icona resta nascosta. Solo i tipi evento la mostrano.
        /// </summary>
        public void Reveal(TileType type)
        {
            bool isEventType = type != TileType.Strada && type != TileType.Neutra;

            if (isEventType && _icon != null && _visualConfig != null)
            {
                _icon.sprite = _visualConfig.GetIcon(type);
                _icon?.gameObject.SetActive(true);
            }
            else
            {
                _icon?.gameObject.SetActive(false);
            }

            _alpha?.gameObject.SetActive(true);
        }
    }
}
