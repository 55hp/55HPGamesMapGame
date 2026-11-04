using UnityEngine;
using UnityEngine.UI;

namespace hp55games.MapGame.Features.UI.Menu
{
    /// <summary>
    /// Spunta del menu "Boss visibile" per la Nebbia (D-043, sottomodalità). Ricorda la scelta (PlayerPrefs)
    /// e la scrive nelle impostazioni della prossima partita.
    /// </summary>
    public sealed class FogBossToggle : MonoBehaviour
    {
        private const string Key = "mapgame.fog_show_boss";

        [SerializeField] private Toggle _toggle;

        private void Awake()
        {
            if (_toggle == null)
            {
                Debug.LogError("[FogBossToggle] _toggle non assegnato.", this);
                enabled = false;
                return;
            }
            _toggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(Key, 0) == 1);
            _toggle.onValueChanged.AddListener(OnChanged);
        }

        private void OnEnable()
        {
            if (_toggle != null) MenuRunSetup.Get().FogShowBoss = _toggle.isOn;
        }

        private void OnDestroy()
        {
            if (_toggle != null) _toggle.onValueChanged.RemoveListener(OnChanged);
        }

        private void OnChanged(bool on)
        {
            PlayerPrefs.SetInt(Key, on ? 1 : 0);
            PlayerPrefs.Save();
            MenuRunSetup.Get().FogShowBoss = on;
        }
    }
}
