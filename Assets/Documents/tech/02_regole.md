# 02 — Regole (MapGame v2, livello 1)

Per gli agenti questo file è il GDD. Un **[DEFAULT]** è una scelta provvisoria di Athena in attesa di Franci (elenco completo in `08_decisioni-default.md`): va implementata com'è scritta. Un **[APERTO]** è una decisione rimandata di proposito: non inventare, lascia un `// TODO R-xxx` e scrivi la domanda nel report.

Il vecchio GDD (Notion, congelato all'08/08/2026) non è più una fonte: dove contraddice questo file, vale questo file.

## 1. Pilastri

- **P2** Le scelte del giocatore sono informate, non a caso cieco.
- **P3** Le conseguenze sono permanenti: niente undo, niente fuga, tile girata = non più interagibile.
- **P4** Ogni rivelazione è rischio e opportunità; il giocatore decide sempre se fermarsi o rischiare ancora.
- **P5** La plancia ha una direzione, data dall'obiettivo della missione.
- **P6** La narrazione è veicolo di deduzione (nanogram con indizi narrativi), non decorazione.
- P1 ("ogni reveal informa sugli adiacenti") è **caduto**: la scelta è libera su qualsiasi tile.

## 2. Plancia e azione base

- **R-001** Griglia esagonale 10×10 = 100 tile, coordinate assiali (Q,R) in memoria, conversione odd-q per il disegno. Etichette: colonne A–J, righe 1–10.
- **R-002** Il giocatore può cliccare **qualsiasi** tile coperta. Con m = tile totali e n = tile già scoperte, le scelte sono m − n. Nessun vincolo di adiacenza.
- **R-003** Una tile girata non è più interagibile, senza eccezioni (neppure i forzieri).
- **R-004** Una risorsa ottenuta oltre il massimo è persa.
- **R-005** Le tile **Vuote** sono scoperte fin dall'inizio e non sono cliccabili.
- **R-006** Le Strade (Road) non esistono più nel livello 1. Cascate, cluster, PathCluster ed EventCluster non si usano.
- **R-007** Il posizionamento è sparso e pseudocasuale con seed (stesso seed = stessa mappa): vedi `04_mappa.md`. Niente cluster.
- **R-008** Il punto di partenza è la prima tile esplorata (n = 1 più le Vuote). Piazzamento **come nel codice attuale** (`MapClusterGenerator.PlaceStart/PlaceEnd`, da portare in Rules senza cambiarne il comportamento): la partenza cade a caso tra le tile ad almeno `StartMinBorderDistance` (3) dal bordo; il Boss va alla tile più vicina alla distanza (in esagoni) estratta con i pesi `EndDistanceWeights` (4,5,6,7,8 con pesi 1,2,3,2,1). Il Boss è **nascosto** come ogni altra tile coperta (decisione di Franci, D-025: diverso dal codice attuale, dove il Boss parte Spotted; `RevealInitialTiles` non lo deve più rivelare).
- **R-009** Il numero di tile per tipo è fisso e uguale a ogni partita (censimento in `03_contenuti.md`): niente casualità sulla composizione, solo sulla posizione.

## 3. Il giocatore

- **R-020** HP iniziali 5, massimo 5 (alzabile col level up, R-042).
- **R-021** Sconfitta (unica per morte): HP < 1.
- **R-022** Il giocatore ha **un solo parametro di combattimento, l'Attacco**. Non esiste una Difesa del giocatore. Il bersaglio ha solo Difesa.
- **R-023** Attacco = Attacco base (0) + bonus Arma + bonus Anello + bonus Trinket + potenziamento del Fabbro + level up spesi sull'Attacco. Tutti i bonus sono cumulabili, senza tetto.
- **R-024** Slot di equipaggiamento: Arma, Anello, Trinket, uno ciascuno. Si parte col **Coltello** (Arma di livello 1, +1 Attacco). Il bonus di un'arma di livello N è +N.
- **R-025** Equipaggiare un oggetto in uno slot occupato fa **perdere definitivamente** quello vecchio. Vale per Arma, Anello e Trinket. Se lo slot è vuoto l'oggetto si equipaggia in automatico.
- **R-026** Le monete si guadagnano con le uccisioni e da alcune tile e si spendono con gli NPC. Non esiste metaprogressione fuori dalla run in questa versione **[APERTO]**.
- **R-027** Il **Cibo non esiste**. Muoversi (cliccare) non costa nulla.
- **R-028** Chiavi: due flag di run, Chiave d'argento e Chiave d'oro (R-053). Non si consumano.
- **R-029** Il potenziamento del Fabbro (`03_contenuti.md` §3) è un bonus del personaggio, non dell'arma: +1 Attacco permanente, acquistabile una sola volta.

## 4. Incontri con i mostri

- **R-030** Un mostro non attacca mai di sua iniziativa. L'incontro avviene quando il giocatore clicca la tile.
- **R-031** L'incontro è **un solo scambio, immediato e definitivo**; non c'è un secondo tentativo.
- **R-032** Se Attacco ≥ Difesa: il mostro muore, il giocatore ottiene la ricompensa (monete, XP, eventuale oggetto), non subisce danno.
- **R-033** Se Attacco < Difesa: il mostro fugge, la tile è consumata, nessuna ricompensa, e il giocatore subisce `Difesa − Attacco` di danno.
- **R-034** Formula unica del danno: `max(0, Difesa − Attacco)`. Un risultato a favore del giocatore non cura mai.
- **R-035** Il giocatore non può fuggire né rifiutare un incontro dopo il click.
- **R-036** Mostri e Difese (valori fissi, uguali in ogni run):

| Mostro | Difesa | Monete | XP |
|---|---|---|---|
| Piccolo 1 | 2 | 2 | 1 |
| Piccolo 2 | 3 | 3 | 2 |
| Mini-boss | 5 | vedi R-037 | 3 |
| Boss | 8 | nessuna | nessuna |

- **R-037** Ogni mini-boss ha una ricompensa fissa diversa, definita dal suo contenuto (`03_contenuti.md` §4): arma di livello 5, Chiave d'argento, Chiave d'oro, oppure 5 monete (due mini-boss).
- **R-038** Il bonus "+1 moneta" del level up (R-042) si applica **a ogni guadagno di monete** (ricompense dei mostri, mini-boss, forzieri, monete da uccisione di NPC): per ogni scelta fatta, +1 moneta in più per evento di guadagno. Non riduce i prezzi e non si applica agli acquisti. Deciso da Franci (D-007); l'interpretazione "per evento" e non "per moneta" è un **[DEFAULT]**.
- **R-039** Uccidere il Boss con Attacco ≥ 8 vince la partita (R-072). Se il Boss fugge (Attacco < 8) la partita finisce con game over.

## 5. Progressione

- **R-040** Ogni mostro ucciso dà XP secondo R-036. Il Boss non dà XP.
- **R-041** Completare una riga o una colonna intera (tutte le tile scoperte, Vuote comprese) dà **+1 XP** una tantum per linea. Completarne due con la stessa mossa dà +2.
- **R-042** Ogni 10 XP il giocatore sale di livello e sceglie **una** tra tre opzioni: +1 Attacco, +1 moneta a ogni guadagno (R-038), +1 HP massimo. Le tre opzioni sono sempre tutte disponibili, i bonus si cumulano, non c'è livello massimo.
- **R-043** Il +1 HP massimo non cura: alza soltanto il tetto (HP attuali invariati). **[DEFAULT D-008]**
- **R-044** (Informativo) XP massimo ottenibile: 15×1 + 10×2 + 5×3 = 50 dalle uccisioni, più al massimo 20 dalle linee = 70, cioè fino al livello 7. Va ricontrollato a ogni modifica del censimento.

## 6. Altre tile

- **R-050** **Trappola**: nessun confronto Attacco/Difesa, danno fisso (Trappola livello 1: 1 HP, livello 2: 2 HP **[DEFAULT D-009]**), nessuna ricompensa. Gli Attrezzi del Cacciatore (`03_contenuti.md` §6) la disinnescano.
- **R-051** **Fontana**: una sola per mappa, cura gli HP al massimo, consumata.
- **R-052** **Arbusto**: +1 HP (una bacca), consumato.
- **R-053** **Forziere**: semplice (si apre al click), d'argento (richiede la Chiave d'argento), d'oro (richiede la Chiave d'oro). Contenuti in `03_contenuti.md` §5.
- **R-054** Cliccare un forziere d'argento o d'oro **senza la chiave** lo fa svanire: la tile è consumata e il contenuto è perso (nessuna eccezione a R-003). Per aprirlo serve già avere la chiave al momento del click. Deciso da Franci (D-010).
- **R-055** **Vuota**: nessun effetto (R-005).

## 7. NPC

- **R-060** Ogni NPC è una singola tile. Al click si apre un pop-up contestuale con le sue scelte (l'NPC non è mai un cluster).
- **R-061** Gli NPC combattibili hanno 5 HP, usati come Difesa nel confronto con l'Attacco. Sono combattibili: Strega, Mercante, Cacciatore, Fabbro. **Intoccabili** (solo commercio): Locanda, Contadino, Paesani.
- **R-062** All'apertura il giocatore sceglie **una sola** cosa tra le opzioni dell'NPC (attaccare oppure una delle offerte). Poi la tile è consumata (R-003). Se non può permettersi nulla può chiudere senza scegliere e la tile è persa.
- **R-063** Attaccare un NPC con Attacco ≥ 5 lo uccide e dà il premio associato (`03_contenuti.md` §3). Con Attacco < 5 l'NPC fugge senza premio e il giocatore subisce `5 − Attacco` di danno (stessa regola R-033). **[DEFAULT D-011]**
- **R-064** Le offerte di ogni NPC sono in `03_contenuti.md` §3. Prezzi degli indizi: uno 5 monete, due insieme 9.
- **R-065** I due Mercanti hanno lo stesso catalogo ma sono istanze separate: ciascuno ha la propria tile e la propria vita. Uccidere uno non tocca l'altro.
- **R-066** Il Mercante vende l'arma di livello 3 (10 monete) e anche indizi; il bersaglio dei suoi indizi e il resto del catalogo sono **[APERTI]** (`03_contenuti.md` §3).

## 8. Missione e vittoria

- **R-070** Nel livello 1 esistono **6 missioni**, tutte con lo stesso obiettivo (uccidere il Boss) e lo stesso nome base "Caccia": una per ciascun tipo di NPC di livello 5 (Strega, Cacciatore, Fabbro, Locanda, Mercante) più una per i mini-boss. Ogni missione suggerisce di andare da un bersaglio specifico e fa partire il giocatore con **2 indizi su quel bersaglio** (R-090..R-099). Missione scelta a caso dal seed a ogni nuova partita **[DEFAULT D-026]**. Dati: `../data/missions.csv`.
- **R-071** "Raggiungi una tile" è eliminata (con scelta libera non garantisce rischio).
- **R-072** Vittoria: Boss ucciso. Sconfitta: HP < 1 oppure Boss fuggito (R-039). Valido per tutte le missioni.
- **R-073** Il Boss ha `IsObjective = true`. È l'unico nemico con quel flag.

## 9. Mascheramento (livello di difficoltà 1–6)

- **R-080** Ogni tile coperta mostra un bordo colorato che indica il suo **livello di mascheramento** (1–6). È l'informazione base prima del click (P2).
- **R-081** Il livello dipende dal tipo di contenuto, in modo fisso:

| Livello | Contenuti |
|---|---|
| 1 | Piccolo 1, Trappola livello 1, Arbusto, Forziere semplice |
| 2 | Piccolo 2, Trappola livello 2 |
| 3 | Forziere d'argento, Paesani, Contadino |
| 4 | Fontana (unica tile di livello 4, per ora) |
| 5 | Strega, Cacciatore, Fabbro, Locanda, Mercante, Mini-boss, Forziere d'oro |
| 6 | Boss |

- **R-082** Le Vuote e la partenza non hanno bordo (sono già scoperte).
- **R-083** Il livello 2 contiene solo contenuti negativi; il livello 3 solo positivi; il livello 4 solo la Fontana. Accettato da Franci. **Attenzione:** il livello 6 è occupato dal solo Boss e il livello 4 dalla sola Fontana, quindi il bordo li identifica.

## 10. Indizi

Vedi `05_indizi.md` (R-090 e seguenti).

## 11. Fuori scope del livello 1

Metaprogressione fuori dalla run, shop tra una run e l'altra, sequenza di livelli e biomi, altre missioni, struttura della mappa non quadrata, tile Indizio dedicata (quantità 0 per ora, **D-013**), tutorial.

## 12. Numeri

Ogni numero di questo file vive in `RulesConfig` (classe C# pura) e nell'asset che la espone; nessun numero di regola nel codice degli effetti, nella UI o nei test (i test leggono da `RulesConfig`).
