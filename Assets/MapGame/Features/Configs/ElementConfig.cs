using hp55games.Mobile.Core.Config;
using UnityEngine;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Definizione di una singola specie/archetipo di Element (GDD, sezione Tile &amp;
    /// Element Knowledge System + Resources/Coins, decisione 2026-07-23): un asset
    /// ScriptableObject per specie (es. OrcoBase.asset, Volpe.asset), NON un catalogo
    /// unico indicizzato — la raccolta e la risoluzione per tipo/DifficultyLevel sono
    /// responsabilita' di ElementCatalog.
    ///
    /// Divisione con EconomyConfig (decisione 2026-07-23): qui vivono ricompense e drop
    /// del soggetto (CoinReward, FoodRestore, HpRestore); i prezzi dei potenziamenti
    /// shop restano in EconomyConfig perche' non hanno un Element proprietario naturale.
    ///
    /// I valori numerici di default sono segnaposto NON bilanciati: il bilanciamento per
    /// specie e' responsabilita' del designer sull'asset (vedi "Element Catalog — Design
    /// Tables" in Notion, campi verdi = decisioni Franci), mai nel codice.
    /// ATK/DEF arriveranno con il sistema Combat dedicato — non aggiungerli prima.
    /// </summary>
    [CreateAssetMenu(menuName = "MapGame/Element Config", fileName = "ElementConfig")]
    public sealed class ElementConfig : ScriptableObject, IConfigAsset
    {
        [Header("Identita'")]
        [Tooltip("Id logico stabile della specie (es. \"orco_base\", \"volpe\"). Usato per lookup/salvataggi, non cambia dopo la pubblicazione.")]
        public string SpeciesId;

        [Tooltip("Nome mostrato in UI (popup encounter, loot, dialoghi).")]
        public string DisplayName;

        [Header("Classificazione")]
        [Tooltip("TileType che questa specie puo' occupare. Il vincolo tile-Element e' per tipo: se la tile ospita un Element, deve essere del tipo che la tile si aspetta.")]
        public TileType Type;

        [Tooltip("Cosa succede al reveal della tile che ospita questa specie.")]
        public RevealEffect Reveal;

        [Tooltip("DifficultyLevel di appartenenza (1-6). Usato da ElementCatalog per risolvere le specie eleggibili per una tile generata con quel DifficultyLevel.")]
        [Range(1, 6)] public int DifficultyLevel = 1;

        [Header("Economia / drop (segnaposto, bilanciare sull'asset)")]
        [Tooltip("Monete guadagnate sconfiggendo questa specie (solo fightable: Enemy/Miniboss/Boss). 0 per le altre.")]
        [Min(0)] public int CoinReward;

        [Tooltip("Cibo restituito al reveal (es. Goods). 0 se non applicabile.")]
        [Min(0)] public int FoodRestore;

        [Tooltip("HP restituiti al reveal. 0 se non applicabile. NOTA: Fountain ripristina TUTTI gli HP via logica dedicata (HexGridController), non da questo campo.")]
        [Min(0)] public int HpRestore;

        [Header("Visual")]
        [Tooltip("Icona della specie per popup/HUD. Opzionale: se nulla, la UI usa il fallback per-TileType di HexTileConfigCatalog.")]
        public Sprite Icon;
    }
}
