using hp55games.MapGame.Rules;
using hp55games.Mobile.Core.Config;
using UnityEngine;

namespace hp55games.MapGame.Features.Configs
{
    /// <summary>
    /// Espone in Inspector i numeri principali di RulesConfig (spec 0003). Il resto (censimento, NPC,
    /// oggetti) resta quello di RulesConfigDefaults.Level1. Se l'asset manca dal ConfigCatalog,
    /// HexGridController usa i default puri.
    /// </summary>
    [CreateAssetMenu(menuName = "MapGame/Rules Config", fileName = "RulesConfig_Level1")]
    public sealed class RulesConfigAsset : ScriptableObject, IConfigAsset
    {
        [Header("Giocatore")]
        [Min(1)] public int StartHp = 5;
        [Min(1)] public int StartMaxHp = 5;
        [Tooltip("D-031: 1 (il Coltello porta l'Attacco iniziale a 2).")]
        [Min(0)] public int BaseAttack = 1;

        [Header("Progressione")]
        [Min(1)] public int XpPerLevel = 10;
        [Min(0)] public int LineCompleteXp = 1;

        [Header("NPC")]
        [Min(0)] public int NpcDefense = 5;

        public RulesConfig Build()
        {
            var c = RulesConfigDefaults.Level1();
            c.StartHp = StartHp;
            c.StartMaxHp = StartMaxHp;
            c.BaseAttack = BaseAttack;
            c.XpPerLevel = XpPerLevel;
            c.LineCompleteXp = LineCompleteXp;
            c.NpcDefense = NpcDefense;
            return c;
        }
    }
}
