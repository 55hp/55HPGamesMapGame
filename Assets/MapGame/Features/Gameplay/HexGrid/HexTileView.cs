using UnityEngine;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Rappresentazione visiva minimale di una tile: sfondo per stato (Scoperta/Coperta/
    /// CopertaBloccata) + uno slot icona che gli IHintRenderer controllano.
    /// Nessuna logica di gameplay o di scelta contenuti qui: solo rendering.
    ///
    /// Gli sprite assegnati in Inspector sono PLACEHOLDER per il test — verranno
    /// sostituiti con gli asset Isle of Lore 2 quando si passa dal prototipo alla mappa reale.
    /// </summary>
    public sealed class HexTileView : MonoBehaviour
    {
        [Header("Riferimenti")]
        [SerializeField] private SpriteRenderer _background;
        [SerializeField] private SpriteRenderer _icon;

        [Header("Sprite stato (placeholder)")]
        [SerializeField] private Sprite _scopertaSprite;
        [SerializeField] private Sprite _copertaSprite;
        [SerializeField] private Sprite _copertaBloccataSprite;

        [Header("Icone categoria (placeholder)")]
        [SerializeField] private Sprite _unknownIconSprite; // "?"
        [SerializeField] private Sprite _battagliaIconSprite;
        [SerializeField] private Sprite _npcIconSprite;
        [SerializeField] private Sprite _misteryIconSprite;
        [SerializeField] private Sprite _risorsaIconSprite;

        public HexCoord Coord { get; private set; }

        public void Setup(HexCoord coord)
        {
            Coord = coord;
        }

        public void ApplyState(TileState state)
        {
            if (_background != null)
            {
                _background.sprite = state switch
                {
                    TileState.Scoperta => _scopertaSprite,
                    TileState.Coperta => _copertaSprite,
                    TileState.CopertaBloccata => _copertaBloccataSprite,
                    _ => _copertaBloccataSprite
                };
            }

            // Su Scoperta il contenuto è già noto/consumato: l'icona hint non ha più senso.
            if (state == TileState.Scoperta)
                HideIcon();
        }

        public void ShowUnknownIcon() => SetIconSprite(_unknownIconSprite);

        public void ShowCategoryIcon(TileType type)
        {
            SetIconSprite(type switch
            {
                TileType.Battaglia => _battagliaIconSprite,
                TileType.NPC => _npcIconSprite,
                TileType.Mistery => _misteryIconSprite,
                TileType.Risorsa => _risorsaIconSprite,
                _ => null
            });
        }

        public void HideIcon()
        {
            if (_icon != null) _icon.enabled = false;
        }

        private void SetIconSprite(Sprite sprite)
        {
            if (_icon == null) return;

            if (sprite == null)
            {
                _icon.enabled = false;
                return;
            }

            _icon.enabled = true;
            _icon.sprite = sprite;
        }
    }
}
