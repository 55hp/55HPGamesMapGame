using System.Collections.Generic;
using UnityEngine;

namespace hp55games.MapGame.Features.Gameplay.HexGrid
{
    /// <summary>
    /// Colore rappresentativo per TileType, unica fonte per qualunque UI (Editor o
    /// runtime) debba colorare una tile per tipo — nata come tavolozza inline di
    /// MapGenerationConfigEditor (Editor/MapGenerationConfigEditor.cs), estratta qui
    /// perche' anche codice runtime (es. UIPopup_Encounter._background) ne ha bisogno e
    /// l'Editor non e' referenziabile da un'assembly runtime.
    ///
    /// Path/Shop sono alias [Obsolete] di Road/Trader sullo stesso int, quindi qui
    /// restano una voce sola a testa (una Dictionary&lt;TileType,Color&gt; non puo' avere
    /// due chiavi con lo stesso valore sottostante). Boss/Npc/Goods/Miniboss/Chance
    /// restano in tavolozza per sicurezza (dati serializzati vecchi potrebbero ancora
    /// referenziarli, es. LevelConfig_Test_1.asset ha entry Npc/Goods non ancora
    /// sistemate) anche se il generatore non li piazza piu'. Le voci Bush/BeeHive/
    /// TurnipSprout/MoneyBag/Inn/Witch/Farmer/Hunter hanno colori placeholder scelti
    /// solo per restare leggibili accanto agli altri, non dalla palette ufficiale
    /// asset pack — se serve allinearli alla palette ufficiale, non e' stato fatto qui.
    /// </summary>
    public static class TileTypePalette
    {
        public static readonly IReadOnlyDictionary<TileType, Color> Colors = new Dictionary<TileType, Color>
        {
            { TileType.Void,         new Color(0.500f, 0.500f, 0.500f) },  // #808080 — placeholder, vero no-op
            { TileType.Road,         new Color(0.863f, 0.725f, 0.373f) },  // #dcb95f — ex Path
            { TileType.Enemy,        new Color(0.612f, 0.278f, 0.255f) },  // #9c4741 — ex Battaglia
            { TileType.Trap,         new Color(0.369f, 0.251f, 0.639f) },  // #5e40a3 — ex Chance
            { TileType.Fountain,     new Color(0.243f, 0.588f, 0.780f) },  // #3e96c7
            { TileType.Tree,         new Color(0.322f, 0.494f, 0.243f) },  // #527e3e
            { TileType.Key,          new Color(0.816f, 0.706f, 0.235f) },  // #d0b43c
            { TileType.Chest,        new Color(0.616f, 0.443f, 0.196f) },  // #9d7132
            { TileType.Trader,       new Color(0.847f, 0.694f, 0.314f) },  // #d8b150 — ex Shop
            { TileType.Bush,         new Color(0.537f, 0.600f, 0.329f) },  // #889954 — ex Goods
            { TileType.BeeHive,      new Color(0.827f, 0.616f, 0.129f) },  // #d39d21
            { TileType.TurnipSprout, new Color(0.706f, 0.816f, 0.353f) },  // #b4d05a
            { TileType.MoneyBag,     new Color(0.827f, 0.702f, 0.161f) },  // #d3b329
            { TileType.Inn,          new Color(0.314f, 0.694f, 0.847f) },  // #50b1d8 — ex Npc
            { TileType.Witch,        new Color(0.549f, 0.314f, 0.847f) },  // #8c50d8
            { TileType.Farmer,       new Color(0.463f, 0.663f, 0.286f) },  // #76a949
            { TileType.Hunter,       new Color(0.494f, 0.400f, 0.267f) },  // #7e6644
            { TileType.Npc,          new Color(0.314f, 0.694f, 0.847f) },  // #50b1d8 — legacy, vedi nota sopra
            { TileType.Goods,        new Color(0.537f, 0.600f, 0.329f) },  // #889954 — legacy
            { TileType.Chance,       new Color(0.369f, 0.251f, 0.639f) },  // #5e40a3 — legacy
            { TileType.Miniboss,     new Color(0.400f, 0.176f, 0.153f) },  // #662d27 — legacy, piu' scuro di Enemy
            { TileType.Boss,         new Color(0.086f, 0.086f, 0.086f) },  // #161616 — legacy
        };

        /// <summary>Color.white se il tipo non ha ancora una voce in tavolozza.</summary>
        public static Color GetColor(TileType type) =>
            Colors.TryGetValue(type, out var color) ? color : Color.white;
    }
}
