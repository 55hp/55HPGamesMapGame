# 05 — Sistema di indizi (nanogram narrativo)

Pilastro P6. Questo è il sistema che dà al giocatore informazione prima del click oltre al bordo colorato.

## 1. Natura di un indizio

- **R-090** Un indizio è un frammento narrativo (foglietto di lore, stile oggetti di Baldur's Gate), non una carta da gioco. Ogni missione definisce il proprio bersaglio e i 2 indizi di partenza (R-098).
- **R-091** Ogni indizio è legato a un `ClueTarget` (un tipo di contenuto da cercare) e ha un testo narrativo (chiave di localizzazione `clue.<id>`).
- `ClueTarget` ammessi: Boss, MiniBoss, MonstersNormal (Piccolo 1 + Piccolo 2), Witch, Hunter, Smith, Inn, Trader, Bush, Fountain. Non sono bersagli: Contadino, Paesani, Forzieri, Trappole, Vuote. Nessun NPC vende indizi sulla Strega: compare solo negli indizi di partenza della sua missione.

## 2. Uso

- **R-092** Il giocatore sceglie un indizio che possiede e una **riga o colonna**. Il gioco mostra, sul bordo di quella linea, il simbolo del tipo e il **numero** di tile di quel tipo presenti sulla linea (0, 1, 2…). Come i numeri a bordo griglia di un nanogram.
- **R-093** Il conteggio è sul contenuto originale della linea (fissato alla generazione), indipendentemente da quante tile siano già scoperte.
- **R-094** Un solo slot attivo per riga e uno per colonna. Una volta usato un indizio su una linea, quello slot resta occupato per tutta la partita (D-016, confermato). Al massimo 10 righe + 10 colonne = 20 indizi utili.
- **R-095** Un indizio si usa **subito** appena ottenuto (D-017, Franci): il gioco chiede di scegliere la riga o colonna; non esiste una scorta di indizi da usare dopo. Se se ne ottengono due insieme (acquisto "due per 9", o i 2 indizi di partenza) le scelte di linea si fanno **in catena**, una dopo l'altra.
- **R-096** Se tutte le linee utili sono occupate (nessuno slot libero) l'indizio è perso. Se la scelta è obbligatoria (non si può annullare), un indizio ottenuto non resta mai in sospeso.

## 3. Fonti

- **R-097** (a) set di partenza della missione; (b) acquisto dagli NPC (`03_contenuti.md` §3); (c) tile Indizio dedicata (quantità 0 per ora, D-013).
- **R-098** Indizi di partenza: dipendono dalla missione (R-070): **2 indizi sul bersaglio della missione** (uno dei 5 tipi di NPC di livello 5 oppure i mini-boss), scelti in catena a inizio partita (D-018, Franci).
- **R-099** Le offerte "due indizi insieme" consegnano due indizi dello stesso bersaglio (uno per due linee diverse), scelti in catena.

## 4. Modello dati

```
ClueTarget   : enum { Boss, MiniBoss, MonstersNormal, Witch, Hunter, Smith, Inn, Trader, Bush, Fountain }
ClueDef      : { Id, Target, TextKey }
ClueBoardState : conteggi per (Target, Row) e (Target, Col)            // precalcolati dal generatore
                 slot usati: HashSet<int> rows, HashSet<int> cols
                 risultati mostrati: Dictionary<LineRef, (Target, Count)>
RunState.PendingClueChoices : coda di ClueDef da assegnare a una linea (di norma 0–2)
```

Logica pura in Rules: `ClueService.Use(clueId, line)` → evento `ClueUsed(line, target, count)` oppure errore (`SlotOccupied`, `NoPendingClue`). La UI non calcola nulla.

## 5. UI (vincoli mobile)

- Su ogni riga e su ogni colonna, **al più un simbolo e un numero** (R-092/R-094): niente stack di etichette, pensato per un pollice su schermo da 8".
- Quando c'è un indizio in attesa, la UI mostra il testo narrativo e chiede di toccare l'intestazione di una riga o colonna libera (R-095). Un pannello "Indizi" (sola lettura) elenca quelli già usati con testo e linea.
- Zoom e pan: le etichette di bordo restano agganciate alla griglia (vedi `06_presentazione.md`).

## 6. Test

Casi in `07_test-e-simulazione.md` §2: conteggi corretti su mappe di 1000 seed, slot unico per linea, determinismo.
