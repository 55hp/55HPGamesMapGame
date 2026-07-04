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
    /// Gli sprite assegnati in Inspector sono PLACEHOLDER — verranno
    /// sostituiti con gli asset Isle of Lore 2 quando si passa al prototipo reale.
    /// </summary>
    public sealed class HexTileView : MonoBehaviour
    {
        [Header("Riferimenti")]
        [SerializeField] private SpriteRenderer _background;
        [SerializeField] private SpriteRenderer _icon;
        [SerializeField] private SpriteRenderer _alpha;

        [Header("Sprite stato non-risolto (placeholder)")]
        [SerializeField] private Sprite _sconosciutaSprite;
        [SerializeField] private Sprite _conosciutaSprite;

        [Header("Indicatore clickability (child GameObject, wired in prefab)")]
        [SerializeField] private GameObject _clickableIndicator;

        [Header("Sprite ambiente scoperto (placeholder)")]
        [SerializeField] private Sprite _stradaSprite;
        [SerializeField] private Sprite _battagliaSprite;
        [SerializeField] private Sprite _trappolaSprite;
        [SerializeField] private Sprite _risorsaSprite;
        [SerializeField] private Sprite _npcSprite;
        [SerializeField] private Sprite _misterySprite;
        [SerializeField] private Sprite _bossSprite;

        public HexCoord Coord { get; private set; }

        public void Setup(HexCoord coord)
        {
            Coord = coord;
        }

        /// <summary>
        /// Aggiorna lo sprite del background in base allo stato di conoscenza.
        /// Sconosciuta → nuvole (nessuna informazione).
        /// Conosciuta e Scoperta → arte del bioma (_conosciutaSprite, oggi un unico
        /// placeholder; in futuro varierà per cluster/bioma). Il background NON
        /// cambia mai al reveal: l'ambiente è il sistema di hint e resta lo stesso
        /// prima e dopo aver giocato l'evento.
        /// </summary>
        public void ApplyState(TileState state)
        {
            if (_background == null) return;

            _background.sprite = state switch
            {
                TileState.Sconosciuta => _sconosciutaSprite,
                _                     => _conosciutaSprite,
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
        /// Mostra il record dell'evento risolto: attiva l'icona (tipo specifico)
        /// e l'overlay alpha (marca la tile come parte del set già rivelato).
        /// Non è un hint — arriva solo dopo l'interazione del giocatore.
        /// Background invariato: l'ambiente resta quello del bioma già mostrato
        /// da Conosciuta.
        /// </summary>
        public void Reveal(TileType type)
        {
            if (_icon != null)
            {
                _icon.sprite = type switch
                {
                    TileType.Strada    => _stradaSprite,
                    TileType.Battaglia => _battagliaSprite,
                    TileType.Trappola  => _trappolaSprite,
                    TileType.Risorsa   => _risorsaSprite,
                    TileType.NPC       => _npcSprite,
                    TileType.Mistery   => _misterySprite,
                    TileType.Boss      => _bossSprite,
                    _                  => null
                };
            }

            _icon?.gameObject.SetActive(true);
            _alpha?.gameObject.SetActive(true);
        }
    }
}
