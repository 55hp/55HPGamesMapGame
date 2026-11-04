using hp55games.MapGame.Features.Gameplay.Run;
using hp55games.MapGame.Rules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace hp55games.MapGame.Features.UI.Run
{
    /// <summary>HUD v2 (06 §2): HP, Attacco, monete, livello/XP, chiavi, equipaggiamento, obiettivo.</summary>
    public sealed class RunHudView : MonoBehaviour
    {
        [Header("Barra superiore")]
        [SerializeField] private TextMeshProUGUI _hp;
        [SerializeField] private TextMeshProUGUI _attack;
        [SerializeField] private TextMeshProUGUI _coins;
        [SerializeField] private TextMeshProUGUI _level;
        [SerializeField] private TextMeshProUGUI _xp;
        [Tooltip("Image di tipo Filled: riempimento verso il prossimo livello.")]
        [SerializeField] private Image _xpFill;
        [SerializeField] private GameObject _silverKey;
        [SerializeField] private GameObject _goldKey;
        [SerializeField] private TextMeshProUGUI _goal;
        [Tooltip("Facoltativo: bonus monete del level up; si nasconde se 0.")]
        [SerializeField] private TextMeshProUGUI _coinBonus;

        [Header("Barra inferiore (equipaggiamento)")]
        [SerializeField] private TextMeshProUGUI _weapon;
        [SerializeField] private TextMeshProUGUI _ring;
        [SerializeField] private TextMeshProUGUI _trinket;

        private void Awake()
        {
            if (_hp == null || _attack == null || _coins == null || _level == null || _xp == null || _xpFill == null
                || _silverKey == null || _goldKey == null || _goal == null || _weapon == null || _ring == null || _trinket == null)
            {
                Debug.LogError("[RunHudView] Riferimenti mancanti nel prefab.", this);
                enabled = false;
            }
        }

        public void Bind(IRunSession session) { }

        public void Refresh(RulesEngine e)
        {
            if (!enabled) return;
            var s = e.State;
            var c = e.Config;
            _hp.text = RunText.T("run.hud.hp", Mathf.Max(0, s.Hp), s.MaxHp); // il motore può scendere sotto 0: a schermo minimo 0
            _attack.text = RunText.T("run.hud.attack", s.Attack);
            _coins.text = RunText.T("run.hud.coins", s.Coins);
            _level.text = RunText.T("run.hud.level", s.Level);
            int levelStart = (s.Level - 1) * c.XpPerLevel;
            _xp.text = RunText.T("run.hud.xp", s.Xp - levelStart, c.XpPerLevel);
            _xpFill.fillAmount = Mathf.Clamp01((s.Xp - levelStart) / (float)c.XpPerLevel);
            _silverKey.SetActive(s.HasSilverKey);
            _goldKey.SetActive(s.HasGoldKey);
            _goal.text = RunText.T("run.hud.goal", c.Content(ContentKind.Boss).Defense);
            if (_coinBonus != null)
            {
                _coinBonus.gameObject.SetActive(s.CoinBonusPerGain > 0);
                _coinBonus.text = RunText.T("run.hud.coin_bonus", s.CoinBonusPerGain);
            }
            _weapon.text = RunText.Item(s.Weapon?.Id);
            _ring.text = RunText.Item(s.Ring?.Id);
            _trinket.text = RunText.Item(s.Trinket?.Id);
        }
    }
}
