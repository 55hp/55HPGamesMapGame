// hp55games.Ui
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using hp55games.Mobile.Core.UI;

namespace hp55games.Ui
{
    public class UIPopup_Generic : UIPopupBase
    {
        public CanvasGroup cg;
        public TMP_Text title;
        public TMP_Text body;
        public Button confirm;
        public Button cancel;

        public void Open(string t, string b, System.Action onConfirm, System.Action onCancel = null)
        {
            title.text = t; body.text = b;

            Bind(confirm, () => { onConfirm?.Invoke(); ClosePopup(); });
            if (cancel != null)
                Bind(cancel, () => { onCancel?.Invoke(); ClosePopup(); });

            cg.alpha = 1; cg.blocksRaycasts = true; cg.interactable = true;
        }

        /// <summary>Wrapper pubblico per compatibilità: chiude sempre passando dal servizio (ClosePopup), non solo abbassando l'alpha come prima.</summary>
        public void Close() => ClosePopup();
    }
}
