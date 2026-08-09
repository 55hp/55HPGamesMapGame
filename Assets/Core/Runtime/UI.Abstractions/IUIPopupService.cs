using System;
using System.Threading.Tasks;
using UnityEngine;

namespace hp55games.Mobile.Core.UI
{
    public interface IUIPopupService
    {
        /// <summary>
        /// <paramref name="configure"/>, se presente, viene invocato mentre il popup è
        /// ancora disattivato (subito dopo l'istanziazione, prima dello scrim fade e di
        /// qualunque frame renderizzato) — così chi configura dati/visual (icone, colori,
        /// testi) non rischia di mostrare per un frame i placeholder assegnati in
        /// Inspector prima dei valori reali. Il popup torna attivo solo dopo che
        /// <paramref name="configure"/> è tornato.
        /// </summary>
        Task<GameObject> OpenAsync(string address, Action<GameObject> configure = null);
        Task<T> OpenAsync<T>(string address, Action<T> configure = null) where T : Component;
        void Close(GameObject popup);
        void CloseTop();
        void CloseAll();
    }
}