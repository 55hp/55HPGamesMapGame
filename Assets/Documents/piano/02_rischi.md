# 02 — Rischi

| # | Rischio | Prob. | Impatto | Mitigazione |
|---|---|---|---|---|
| 1 | Il Boss (Difesa 8) è troppo facile o impossibile: arma L5 + Anello +2 + Fabbro = 8 | alta | alto | bot di bilanciamento (0012), numeri in `RulesConfig` |
| 2 | Click libero + info scarsa = gioco a caso, non deduzione (P2) | media | alto | indizi di partenza, bordo di colore; verificare in M6 |
| 3 | Livello 2 tutto negativo: scelta obbligata | media | medio | ricontrollare col bot, D-012 |
| 4 | Etichette di nanogram illeggibili su schermo mobile | media | medio | un solo simbolo+numero per linea; test su dispositivo |
| 5 | Rimozione di Food rompe riferimenti in scene/prefab | alta | basso | checklist Bezi in 0005 |
| 6 | Assembly Rules puro duplica tipi con Features | media | medio | `ContentKindMap` unico punto; D-003 |
| 7 | Report di Bezi vecchi o contraddittori col disco | media | medio | regola di verifica commit (CLAUDE.md) |
| 8 | Scope creep (metaprogressione, più missioni) | media | alto | §11 di `02_regole.md` fuori scope |
| 9 | Nessun salvataggio: run persa se l'app va in background | bassa | basso | D-023, rivedere dopo M6 |
| 10 | Pochi test esistenti nel repo: regressioni sul codice legacy | media | basso | test nuovi solo su Rules |
