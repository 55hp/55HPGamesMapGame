# 07 — Test e simulazione

## 1. Principi

Ogni regola `R-xxx` implementata ha almeno un test che ne porta l'ID nel nome. I test leggono i numeri da `RulesConfig`, mai letterali. Rules si testa con `dotnet test Tools/RulesHarness` (§ architettura 3).

## 2. Elenco minimo dei test di regole

| Area | Casi |
|---|---|
| Scambio (R-031..034) | Attacco = Difesa → uccide, danno 0; Attacco > Difesa → danno 0, nessuna cura; Attacco < Difesa → mostro fugge, danno = Difesa − Attacco, nessuna ricompensa; tile consumata in ogni caso |
| Attacco (R-023..025) | somma dei bonus; sostituzione di slot perde il vecchio; slot vuoto equipaggia da solo; Fabbro una sola volta |
| HP (R-020,021,043) | clamp al massimo; HP < 1 → sconfitta; +1 HP max non cura |
| XP (R-040..044) | XP per mostro; +1 per riga/colonna completata (doppia = +2); livello ogni 10 XP; più livelli in una mossa; scelte cumulabili |
| Bonus monete (R-038) | +1 per scelta a ogni evento di guadagno (mostri, mini-boss, forzieri, uccisione NPC); non agli acquisti; cumulabile |
| Forzieri (R-053,054) | semplice si apre; senza chiave svanisce, contenuto perso, tile consumata; con chiave si apre; nessun forziere è riapribile |
| NPC (R-060..065) | una sola scelta; prezzo non accessibile disabilitato; attacco ≥5 ucciso con premio; <5 fugge e danno; due Mercanti indipendenti |
| Indizi (05) | conteggio corretto per ogni linea su 1000 seed; slot unico per linea; uso immediato e catena di 2; `Use` senza indizio in attesa → errore; indizio perso se nessuno slot libero; determinismo |
| Missioni (R-070) | 6 missioni; ciascuna dà 2 indizi sul bersaglio giusto; scelta determinata dal seed |
| Start/Boss (R-008) | Start ad almeno 3 dal bordo; distanza Boss nei valori di `EndDistanceWeights`; Boss coperto (né Spotted né Explored); stesso seed = stesse posizioni del codice attuale |
| Generatore (04) | somma = 100; conteggi per tipo = censimento; Boss ≠ Start; stesso seed = stessa mappa; seed diversi = mappe diverse; Vuote e Start scoperte |
| Vittoria | Boss con Attacco ≥ 8 → vittoria; < 8 → Boss fugge → sconfitta |
| Determinismo | stesso seed + stesse scelte = stessa sequenza di eventi |

## 3. Bot di bilanciamento (spec 0012)

Un bot puro in Rules, `IPlayerPolicy` con `Choose(IReadOnlyList<LegalOption>)`:
- **RandomPolicy**: sceglie a caso tra le tile coperte e tra le opzioni legali.
- **GreedyPolicy**: preferisce prima i livelli bassi (1→2→3→5→6), usa gli indizi di partenza, acquista armi, affronta il Boss solo con Attacco ≥ 8.

Esegue N partite (default 10 000) per seed e produce un CSV: vittorie, causa della sconfitta, livello massimo, Attacco al Boss, monete spese. Serve a Franci per bilanciare `RulesConfig`.

Domande a cui deve rispondere: la partita è vincibile? Con quale probabilità col Greedy? Il Boss con Attacco 8 è raggiungibile (peso: arma L5 + anello +2 + Fabbro + level up)? I livelli L2 (solo negativi) rendono la scelta obbligata?

## 4. Test Unity

EditMode in `Assets/Tests/EditMode/`: integrazione Features (adapter, ContentKindMap). PlayMode solo smoke della scena di gioco. Li lancia Franci; il report indica quali.

## 5. Simulazione carta e penna (archivio)

I calcoli del censimento (Notion, "Simulazione partita") restano come riferimento storico; le percentuali valide sono in `03_contenuti.md` §1.
