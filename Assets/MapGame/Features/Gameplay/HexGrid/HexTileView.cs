using TMPro;
using UnityEngine;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Rappresentazione visiva di una tile. Cinque componenti separati, sorting order
    /// front-to-back: 1) _border (SpriteRenderer, uguale su ogni tile, colore da
    /// DifficultyLevel), 2) _icon + _iconLabel (SpriteRenderer + TMP, stesso sorting
    /// layer, rappresentano insieme cosa nasconde la tile: icona = Element/evento, testo =
    /// la sua etichetta), 3) _alpha (SpriteRenderer, identico su ogni tile, nessuna
    /// variazione), 4) _background (SpriteRenderer, varia per tipo di ambiente). Sorting
    /// layer/order effettivi restano lavoro Editor — qui solo i riferimenti e la logica
    /// che li pilota.
    ///
    /// ApplyState gestisce l'aspetto non-risolto (Sconosciuta / Conosciuta).
    /// Reveal mostra l'ambiente scoperto: l'arte ambientale è il sistema di hint.
    /// SetClickable attiva/disattiva l'indicatore di clickability.
    /// Nessuna logica di gameplay qui: solo rendering.
    ///
    /// Gli sprite/etichette sono centralizzati in HexTileConfigCatalog (asset condiviso)
    /// invece che assegnati per-istanza qui — un solo posto da aggiornare quando cambia un
    /// tipo o se ne aggiunge uno nuovo, anche con più varianti di prefab in futuro.
    ///
    /// _background "varia per tipo di ambiente": il Three-Axis Visual Model (Bioma/
    /// Ambiente/Variante) è ancora design-only, nessun tipo/enum Ambiente esiste in codice
    /// (vedi MapGameHexDebugSetup.cs). Finche' non esiste, ApplyState continua a usare
    /// Sconosciuta/Conosciuta come sostituto, ma passa sempre da SetBackground: quando un
    /// catalogo per-ambiente arrivera', bastera' chiamare SetBackground con lo sprite
    /// risolto da quello, senza toccare HexTileView.
    /// </summary>
    public sealed class HexTileView : MonoBehaviour
    {
        [Header("Riferimenti — 5 componenti, sorting order front-to-back")]
        [Tooltip("1) Bordo, uguale su ogni tile — colore da DifficultyLevel (SetDifficultyLevel).")]
        [SerializeField] private SpriteRenderer _border;
        [Tooltip("2a) Icona di cosa nasconde la tile (Element/evento) — stesso sorting layer di _iconLabel.")]
        [SerializeField] private SpriteRenderer _icon;
        [Tooltip("2b) Etichetta TMP di cosa nasconde la tile — stesso sorting layer di _icon. TMP, non legacy Text.")]
        [SerializeField] private TextMeshPro _iconLabel;
        [Tooltip("3) Overlay identico su ogni tile, nessuna variazione per-tile in codice.")]
        [SerializeField] private SpriteRenderer _alpha;
        [Tooltip("4) Sfondo, varia per tipo di ambiente — vedi SetBackground.")]
        [SerializeField] private SpriteRenderer _background;

        [Header("Configurazione visiva condivisa")]
        [SerializeField] private HexTileConfigCatalog _visualConfig;

        [Header("Indicatore clickability (child GameObject, wired in prefab)")]
        [SerializeField] private GameObject _clickableIndicator;

        public HexCoord Coord { get; private set; }

        public void Setup(HexCoord coord)
        {
            Coord = coord;
        }

        /// <summary>
        /// Aggiorna lo sprite del background in base a SpottingState.
        /// Unspotted → nuvole (nessuna informazione).
        /// Spotted → arte del bioma (copre sia il vecchio Conosciuta che Scoperta). Il
        /// background NON cambia mai al reveal (ExplorationState non serve qui): l'ambiente
        /// è il sistema di hint e resta lo stesso prima e dopo aver giocato l'evento.
        /// Sostituto temporaneo finche' non esiste un tipo Ambiente vero (vedi doc di
        /// classe) — passa comunque da SetBackground, non da _background diretto.
        /// </summary>
        public void ApplyState(SpottingState spotting)
        {
            if (_visualConfig == null) return;

            SetBackground(spotting switch
            {
                SpottingState.Unspotted => _visualConfig.SconosciutaSprite,
                _                       => _visualConfig.ConosciutaSprite,
            });

            _icon?.gameObject.SetActive(false);
            _iconLabel?.gameObject.SetActive(false);
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
        /// Colore del bordo (componente 1) da DifficultyLevel. Chiamabile per ogni tile
        /// indipendentemente da Exploration/Spotting: il DifficultyLevel è noto dalla
        /// generazione (vedi HexTileData.DifficultyLevel), non solo dopo il reveal — se il
        /// bordo debba restare nascosto finche' la tile non e' Conosciuta/Scoperta e' una
        /// decisione di visibilita' Editor (attiva/disattiva _border sul prefab), non di
        /// questo metodo: qui si aggiorna solo il colore.
        /// </summary>
        public void SetDifficultyLevel(int difficultyLevel)
        {
            if (_border == null || _visualConfig == null) return;
            _border.color = _visualConfig.GetDifficultyLevelColor(difficultyLevel);
        }

        /// <summary>
        /// Sfondo (componente 4). Punto unico da cui _background viene scritto — vedi doc
        /// di classe sul perche' non e' ancora guidato da un tipo Ambiente.
        /// </summary>
        private void SetBackground(Sprite sprite)
        {
            if (_background != null) _background.sprite = sprite;
        }

        /// <summary>
        /// Mostra icona (componente 2a) ed etichetta (componente 2b) insieme per una tile
        /// visibile (Conosciuta o Scoperta) — stessa condizione showIcon per entrambe, gia'
        /// valutata dal chiamante (vero sempre per Scoperta, per Conosciuta solo quando il
        /// numero di vicini Scoperta raggiunge il DifficultyLevel della tile — vedi
        /// HexGridController.CountScopertaNeighbors e HexGridViewSpawner). showAlpha attiva
        /// l'overlay "già giocata" (componente 3) — true solo per le tile Scoperta. Icona ed
        /// etichetta restano nascoste singolarmente se GetIcon/GetLabel non hanno ancora
        /// un valore per quel tipo (nessuna HexTileConfig assegnata, o Label vuota).
        /// </summary>
        public void Reveal(TileType type, bool showIcon, bool showAlpha = false)
        {
            if (_visualConfig != null)
            {
                bool show = showIcon;

                if (_icon != null)
                {
                    var icon = _visualConfig.GetIcon(type);
                    _icon.sprite = icon;
                    _icon.gameObject.SetActive(show && icon != null);
                }

                if (_iconLabel != null)
                {
                    var label = _visualConfig.GetLabel(type);
                    _iconLabel.text = label;
                    _iconLabel.gameObject.SetActive(show && !string.IsNullOrEmpty(label));
                }
            }

            if (showAlpha)
                _alpha?.gameObject.SetActive(true);
        }
    }
}
