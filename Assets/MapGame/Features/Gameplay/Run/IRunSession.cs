using System;
using System.Collections.Generic;
using hp55games.MapGame.Rules;
using UnityEngine;

namespace hp55games.MapGame.Features.Gameplay.Run
{
    /// <summary>
    /// Partita in corso vista dalla UI (registrata nel ServiceRegistry da RunSession, sul GameObject della griglia).
    /// La UI legge Engine e gli eventi; agisce solo con Submit/ConfirmEnd (regola d'oro 3).
    /// </summary>
    public interface IRunSession
    {
        /// <summary>Null finché la partita non è generata.</summary>
        RulesEngine Engine { get; }

        /// <summary>Eventi di regola di ogni azione, nell'ordine del motore.</summary>
        event Action<IReadOnlyList<RuleEvent>> RuleEventsRaised;

        /// <summary>Nuova partita generata (la UI azzera il proprio stato).</summary>
        event Action SessionStarted;

        bool Submit(string optionId);

        /// <summary>La partita è finita ma la scena non è ancora passata ai risultati: la UI mostra l'esito.</summary>
        bool IsAwaitingEndConfirm { get; }

        /// <summary>Chiude l'esito e passa alla schermata dei risultati.</summary>
        void ConfirmEnd();

        /// <summary>Per qualche frame i tocchi non scoprono tile (il tocco apparteneva a un pulsante UI).</summary>
        void BlockBoardInput();

        /// <summary>Posizione a schermo (pixel) del centro della tile (col, riga) in coordinate di regola.</summary>
        bool TryGetScreenPoint(int col, int row, out Vector2 screenPoint);
    }

    /// <summary>Marcatore: una UI definitiva della partita è attiva (la UI di prototipo si spegne).</summary>
    public interface IRunHud { }
}
