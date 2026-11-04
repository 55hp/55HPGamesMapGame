# Bilanciamento: confronti tra versioni

Ogni sezione è un'esecuzione di `Tools/RulesHarness/run.sh --compare <partite> Assets/Documents/data/balance/confronti.md "<etichetta>" [Nome=valore ...]`,
con gli stessi seed (0..N-1) per tutte le righe, così le differenze vengono dalle regole e non dalla fortuna.

Bot:
- **random**: tocca e sceglie a caso tra le mosse legali.
- **greedy**: legge solo i bordi (livello di mascheramento) e lo stato; ignora gli indizi.
- **clue**: come greedy, ma stima dagli indizi dove sono mini-boss, NPC, arbusti e mostri e posa gli indizi sulla linea con più candidati.

Colonne:
- **Tocchi ciechi**: tocchi su tile coperte senza bordo visibile (solo Nebbia).
- **Mosse legali medie**: quante tile si possono toccare, in media, a ogni scelta.
- **Scelte con mossa sicura**: percentuale di scelte in cui c'era almeno una tile visibile di livello 0, 1, 3 o 4 (rischio massimo -1 HP). 100% = non c'è mai una scelta davvero rischiosa obbligata (pilastro P4 debole).
- **Indizi extra ricevuti**: oltre ai 2 di partenza (linee completate, Piccoli 2, acquisti).
