using System.Collections.Generic;
using hp55games.MapGame.Features.Gameplay.Run;
using hp55games.MapGame.Rules;
using TMPro;
using UnityEngine;

namespace hp55games.MapGame.Features.UI.Run
{
    /// <summary>
    /// Pannello delle scelte (06 §4): NPC, level up, oggetto trovato, indizio da posare.
    /// Per l'indizio mostra solo titolo e istruzione: la linea si sceglie toccando le etichette di bordo.
    /// Le righe-opzione sono istanziate dal prefab assegnato (oggetti generati dai dati, regola 6).
    /// </summary>
    public sealed class DecisionPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private TextMeshProUGUI _body;
        [SerializeField] private Transform _optionsContainer;
        [SerializeField] private DecisionOptionView _optionPrefab;

        private readonly List<DecisionOptionView> _pool = new List<DecisionOptionView>();
        private IRunSession _session;

        private void Awake()
        {
            if (_root == null || _title == null || _body == null || _optionsContainer == null || _optionPrefab == null)
            {
                Debug.LogError("[DecisionPanelView] Riferimenti mancanti nel prefab.", this);
                enabled = false;
                return;
            }
            _root.SetActive(false);
        }

        private void Update()
        {
            // I tocchi sul pannello non devono scoprire la tile sottostante.
            if (_root.activeSelf) _session?.BlockBoardInput();
        }

        public void Bind(IRunSession session) => _session = session;

        public void Refresh(RulesEngine e)
        {
            if (!enabled) return;
            var d = e.CurrentDecision;
            _root.SetActive(d != null);
            if (d == null) return;

            _title.text = RunText.DecisionTitle(e, d);
            _body.text = RunText.DecisionBody(e, d);

            int shown = 0;
            if (d.Kind != DecisionKind.ClueLine)
            {
                foreach (var o in d.Options)
                {
                    var view = Get(shown++);
                    string id = o.Id;
                    view.Set(RunText.OptionLabel(e, d, o), RunText.OptionDetail(e, d, o), o.Enabled, () =>
                    {
                        _session?.BlockBoardInput();
                        _session?.Submit(id);
                    });
                }
            }
            for (int i = 0; i < _pool.Count; i++) _pool[i].gameObject.SetActive(i < shown);
        }

        private DecisionOptionView Get(int i)
        {
            while (_pool.Count <= i) _pool.Add(Instantiate(_optionPrefab, _optionsContainer));
            _pool[i].gameObject.SetActive(true);
            return _pool[i];
        }
    }
}
