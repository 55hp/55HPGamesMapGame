using UnityEngine;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Rappresentazione visiva di una tile.
    /// ApplyState gestisce l'aspetto coperto (Coperta / CopertaBloccata).
    /// Reveal mostra l'ambiente scoperto: l'arte ambientale è il sistema di hint.
    /// Nessuna logica di gameplay qui: solo rendering.
    ///
    /// Gli sprite assegnati in Inspector sono PLACEHOLDER — verranno
    /// sostituiti con gli asset Isle of Lore 2 quando si passa al prototipo reale.
    /// </summary>
    public sealed class HexTileView : MonoBehaviour
    {
        [Header("Riferimenti")]
        [SerializeField] private SpriteRenderer _background;

        [Header("Sprite stato coperto (placeholder)")]
        [SerializeField] private Sprite _copertaSprite;
        [SerializeField] private Sprite _copertaBloccataSprite;

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
        /// Aggiorna lo sprite per lo stato coperto. Coperta = raggiungibile, CopertaBloccata = non ancora.
        /// Non gestisce Scoperta: usa Reveal() per quello.
        /// </summary>
        public void ApplyState(TileState state)
        {
            if (_background == null) return;

            _background.sprite = state switch
            {
                TileState.Coperta => _copertaSprite,
                TileState.CopertaBloccata => _copertaBloccataSprite,
                _ => _copertaBloccataSprite
            };
        }

        /// <summary>
        /// Mostra l'ambiente della tile rivelata. L'arte ambientale è il sistema di hint:
        /// non esiste una sovrapposizione icona separata.
        /// </summary>
        public void Reveal(TileType type)
        {
            if (_background == null) return;

            _background.sprite = type switch
            {
                TileType.Strada => _stradaSprite,
                TileType.Battaglia => _battagliaSprite,
                TileType.Trappola => _trappolaSprite,
                TileType.Risorsa => _risorsaSprite,
                TileType.NPC => _npcSprite,
                TileType.Mistery => _misterySprite,
                TileType.Boss => _bossSprite,
                _ => null
            };
        }
    }
}
