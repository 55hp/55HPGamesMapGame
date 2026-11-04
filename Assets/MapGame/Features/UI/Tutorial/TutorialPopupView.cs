using hp55games.MapGame.Features.Gameplay.Run;
using hp55games.MapGame.Features.UI.Run;
using hp55games.Mobile.Core.Architecture;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace hp55games.MapGame.Features.UI.Tutorial
{
    /// <summary>
    /// Tutorial a pagine (UX 5): si apre da solo alla prima partita e poi dal pulsante "?" dell'HUD.
    /// Testi in localization_master.txt: run.tutorial.title.N / run.tutorial.body.N (N = 1.._pageCount).
    /// Mentre è aperto il tabellone non riceve tocchi.
    /// </summary>
    public sealed class TutorialPopupView : MonoBehaviour
    {
        private const string SeenKey = "mapgame.tutorial_seen";

        [SerializeField] private GameObject _root;
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private TextMeshProUGUI _body;
        [SerializeField] private TextMeshProUGUI _pageLabel;
        [SerializeField] private Button _prev;
        [SerializeField] private Button _next;
        [SerializeField] private Button _close;
        [SerializeField] private Button _openButton;
        [SerializeField] private int _pageCount = 6;
        [SerializeField] private bool _autoShowFirstRun = true;

        private IRunSession _session;
        private int _page;

        private void Awake()
        {
            if (_root == null || _title == null || _body == null || _pageLabel == null
                || _prev == null || _next == null || _close == null || _openButton == null)
            {
                Debug.LogError("[TutorialPopupView] Riferimenti mancanti nel prefab.", this);
                enabled = false;
                return;
            }
            _prev.onClick.AddListener(() => Show(_page - 1));
            _next.onClick.AddListener(() => Show(_page + 1));
            _close.onClick.AddListener(Close);
            _openButton.onClick.AddListener(() => Open());
            _root.SetActive(false);
        }

        private void Update()
        {
            if (_session == null || (_session is Object o && o == null)) TryBind();
            if (_root.activeSelf) _session?.BlockBoardInput();
        }

        private void OnDestroy()
        {
            if (_session != null) _session.SessionStarted -= OnSessionStarted;
        }

        private void TryBind()
        {
            _session = null;
            if (!ServiceRegistry.TryResolve(out IRunSession s) || s == null || (s is Object o && o == null)) return;
            _session = s;
            _session.SessionStarted += OnSessionStarted;
            OnSessionStarted();
        }

        private void OnSessionStarted()
        {
            if (_autoShowFirstRun && PlayerPrefs.GetInt(SeenKey, 0) == 0) Open();
        }

        public void Open()
        {
            if (!enabled) return;
            _root.SetActive(true);
            Show(0);
        }

        private void Close()
        {
            _session?.BlockBoardInput();
            _root.SetActive(false);
            PlayerPrefs.SetInt(SeenKey, 1);
            PlayerPrefs.Save();
        }

        private void Show(int page)
        {
            _page = Mathf.Clamp(page, 0, _pageCount - 1);
            int n = _page + 1;
            _title.text = RunText.T("run.tutorial.title." + n);
            _body.text = RunText.T("run.tutorial.body." + n);
            _pageLabel.text = RunText.T("run.tutorial.page", n, _pageCount);
            _prev.interactable = _page > 0;
            _next.gameObject.SetActive(_page < _pageCount - 1);
        }
    }
}
