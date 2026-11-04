# 08 — Registro delle decisioni (default di Athena riviste da Franci)

Stati: **chiusa** (confermata o decisa da Franci, implementare), **aperta** (in attesa di Franci; implementare il [DEFAULT] indicato).
Ultima revisione di Franci: 13/10/2026.

## Chiuse

| ID | Tema | Decisione | Note |
|---|---|---|---|
| D-001 | Censimento | 100 tile, 14 Vuote | confermata |
| D-002 | Forzieri d'argento | 1 (una sola chiave d'argento) | confermata |
| D-004 | Distanza Start–Boss | si mantiene la logica già implementata (distanza pesata 4–8) | R-008, `04_mappa.md` |
| D-005 | Posizione Start e Boss | si mantiene la logica già implementata (Start ≥3 dal bordo, Boss a distanza pesata) | R-008 |
| D-007 | Bonus monete del level up | a ogni guadagno di monete se ne ottiene 1 in più per scelta | R-038; interpretazione "per evento" confermata (D-027) |
| D-008 | +1 HP max | alza il tetto, non cura | confermata |
| D-009 | Trappole | L1 = 1 HP, L2 = 2 HP | confermata |
| D-010 | Forziere senza chiave | svanisce (tile consumata, contenuto perso) | R-054; nessuna eccezione a R-003 |
| D-011 | Attacco fallito a un NPC | l'NPC fugge, danno = 5 − Attacco | confermata |
| D-014 | Attrezzi del Cacciatore | Trinket, permanenti finché equipaggiati; limite di usi rimandato | confermata |
| D-015 | Lanterna | svela solo la tipologia di una tile adiacente mai mostrata; la tile resta cliccabile | come da pagina NPC |
| D-016 | Slot indizio | permanente: 1 per riga e 1 per colonna | confermata |
| D-017 | Uso degli indizi | uso immediato; se due, scelta della linea in catena | R-095 |
| D-018 | Indizi di partenza | dipendono dalla missione: 2 indizi sul bersaglio | R-070, R-098 |
| D-019 | Risoluzione e mappa | si usa ciò che è già implementato; la creazione della mappa riusa il sistema esistente | `04_mappa.md` §4 |
| D-021 | Livello massimo | nessun tetto (pratico 7) | confermata |
| D-023 | Salvataggio | fuori scope; il template ha un `SaveService`, se ne riparla dopo | rimandata |
| D-024 | Seed | si usa l'implementazione esistente (`CurrentRunSeed` se ≠ 0, altrimenti `MapGenerationConfig.Seed`) | `04_mappa.md` |
| D-003 | Architettura | assembly `Rules` puro + `Tools/RulesHarness` | confermata |
| D-006 | Stato di run | `RunState` v2 in Rules; `IGameContextService` perde Food e HasKeyDL* | confermata |
| D-012 | Livello 4 | la Fontana è l'unica tile di livello 4 (per ora) | il bordo di livello 4 e 6 identifica Fontana e Boss |
| D-013 | Tile Indizio dedicata | quantità 0 | confermata |
| D-020 | Catalogo Mercante | per ora arma L3 a 10 monete + indizi; bersaglio e prezzi degli indizi da definire in seguito | accettata così |
| D-022 | `TileType` | estensione in coda, mai rinumerare; varianti in `ContentKind` | confermata |
| D-025 | Boss all'inizio | **nascosto** come le altre tile coperte | diverso dal codice attuale |
| D-026 | Scelta della missione | a caso dal seed a ogni nuova partita | confermata |
| D-027 | Prezzi NPC e bonus monete | prezzi come da pagina NPC; **due indizi insieme = 9 per tutti**; bonus monete per evento di guadagno | confermata |

| D-033 | Boss e mascheramento (annullata da D-037) | Boss al **livello 5**, non più unico al 6 | Franci 03/11/2026; bot Greedy 48% → 38,5% (il bot non usa gli indizi) |
| D-034 | Orientamento e UI definitiva | **portrait**; HUD, scelte, etichette di bordo ed esito nel prefab `UIRunHUD` istanziato da `MapGameGameplayHudService` sotto `UIRoot.hud`; niente pop-up Addressables per la partita v2 | Franci 03/11/2026 (portrait); struttura scelta da Athena |

| D-036 | Monete iniziali e partenza sicura | 5 monete; niente livelli 2/5/6 adiacenti alla partenza; Piccolo 1 o forziere di monete entro 3 | Franci 04/11/2026 |
| D-037 | Boss e mascheramento | Boss di nuovo **unico al livello 6** (annulla D-033) | Franci 04/11/2026; bot Greedy 38,5% → 49% |
| D-038 | Indizi del Mercante e indizi inutili | Mercante: bersaglio di livello 5 estratto tra quelli ancora coperti (annulla D-028); nessun indizio su bersagli già tutti scoperti, né in vendita né in premio | Franci 04/11/2026 |
| D-039 | Indizi come ricompensa | +1 indizio sul bersaglio della missione per linea completata; Piccolo 2 ucciso → 1 indizio sugli Arbusti (mostri e bersaglio: default di Athena) | Franci 04/11/2026; bot Greedy 48,8% |
| D-040 | Modalità di gioco | Classica / Nebbia / Esplorazione, scelte dal menu; "Riprova" mantiene la modalità | Franci 04/11/2026; bot Greedy 41,2% / 43,1% / 43,1% (il bot ignora gli indizi) |
| D-041 | Tile Vuote | coperte, bordo grigio livello 0, nessun effetto, contano per le linee | Franci 04/11/2026 |
| D-042 | Avatar | il personaggio compare sull'ultima tile toccata; l'icona del contenuto sfuma | Franci 04/11/2026, solo presentazione |
| D-043 | Nebbia e Ricognizione | Nebbia senza bordi adiacenti; Ricognizione da 1 o 7 tile; sottomodalità "Boss visibile"; partenza 2×7 + 1×1, linea completata → 1×7, Locanda/Paesani 1 e 5 monete | Franci 04/11/2026 (idea, Boss opzionale); valori iterati da Athena col bot: Greedy 20,8% (Boss nascosto) / 26,6% (Boss visibile) |
| D-044 | Prezzi degli indizi | 4 per uno, 7 per due (circa -20%, proporzione mantenuta) | Franci 04/11/2026: -30% indicativo; nessun effetto misurabile col bot |
| D-045 | Ricognizione di riga/colonna | 7 monete, solo da Locanda e Paesani; si posa toccando una tile della linea o l'etichetta | Franci 04/11/2026; bot con indizi in Nebbia 18,1% → 19,8% |
| D-046 | Ricompensa della missione | raggiungere il bersaglio (aprire quell'NPC o uccidere un mini-boss) dà +1 Attacco permanente, una volta | Franci 04/11/2026; Nebbia 20,8% → 26,4%, Classica ed Esplorazione invariate |
| D-047 | Difesa dei mini-boss | resta 5 | provata 4: Classica 94,6%, Esplorazione 90,3% (con Attacco 4 tutto il livello 5 diventa sicuro) |
| D-048 | Prezzo dei Trinket | 6 monete (Lanterna, Attrezzi del Cacciatore) | Franci 04/11/2026; effetto piccolo |
| D-049 | Nuovi oggetti del Mercante | Bussola (Trinket, 6, solo in Nebbia: bordi delle tile accanto a quelle scoperte), Borsa (Trinket, 6, +1 moneta a ogni guadagno), Mappa del tesoro (4, mostra il tipo di un forziere d'oro coperto, altrimenti d'argento), Amuleto (Anello +1 Attacco, 5) | Franci 04/11/2026; Amuleto a 5 e non 8 perché l'Anello della Strega dà +2 a 8. Bot che compra, Nebbia: 28,7% → 31,5% con tutti e quattro (quasi tutto dalla Bussola); Classica ed Esplorazione invariate, il bot quasi non incontra il Mercante |
| D-050 | Equipaggiamento: due mani + Trinket cumulabili | Due slot mano identici (sostituiscono Arma e Anello): ogni oggetto a una mano va in uno qualsiasi dei due, un'arma a due mani li unisce in un unico slot; un terzo slot raccoglie tutti i Trinket. In mano: armi a una mano (Coltello, Spade, Ascia del Campione), armi a due mani (Arco, Martello, Bastone, Ammazzadraghi: occupano entrambe le mani), oggetti da mano (Anello +2, Amuleto +1, Bussola). Attacco delle mani: l'arma migliore conta per intero, una seconda arma dà +1 se il suo bonus è almeno 2 (il Coltello non conta), un oggetto da mano dà il suo bonus. Trinket (Lanterna, Attrezzi, Borsa) cumulabili, uno per tipo; un Trinket già posseduto non si ricompra. Con le mani piene si sceglie cosa lasciare | Franci 04/11/2026. Con +1 anche dal Coltello Classica sale dal 40,1% al 94,6% (scartato). Valori scelti: Classica 40,1%, Nebbia 28,4%, Esplorazione 43,2% (greedy) |
| D-051 | Fabbro, Mappe, Ascia | Il Potenziamento del Fabbro diventa la Cote del Fabbro, Trinket +1 Attacco (5 monete, una per partita). La Mappa del tesoro diventa due Trinket del Mercante: Mappa d'oro e Mappa d'argento (4 monete ciascuna), ognuna mostra il proprio forziere e compare solo se è ancora coperto. L'Ascia del Campione diventa a due mani | Franci 04/11/2026. Greedy: Classica 40,1%, Nebbia 28,6% (30,7% col bot che compra), Esplorazione 43,2% |
| D-052 | Scudo, Amuleto della Fenice, Erbario | Scudo: equip a 1 mano dei Paesani (6 monete), -1 danno a colpo, si rompe dopo 3 colpi parati [provvisorio]. Amuleto della Fenice: Trinket della Locanda (8), la prima volta che arriveresti a 0 HP resti a 1 e si consuma. Erbario: Trinket del Contadino (4), gli Arbusti curano +1 | Franci 04/11/2026. Greedy con tutti e tre: Classica 45,6%, Nebbia 35,9%, Esplorazione 64,0%. Scudo che non si rompe: Esplorazione 92% |
| D-053 | Indizi in Nebbia | In Nebbia gli indizi di riga/colonna non esistono: niente indizi di partenza della missione, da linee completate o mostri, niente offerte di indizi dagli NPC. Le Ricognizioni restano invariate. Classica ed Esplorazione invariate | Franci 05/11/2026. Usarli valeva circa +0,6 punti, comprarli costava circa 6 punti. Senza indizi, bot greedy invariato entro il rumore (±2), bot probabilistico +3/+7,5 punti |
| D-054 | Ricognizioni senza limite | Comprare una Ricognizione non chiude l'NPC: se ne comprano finché ci sono monete. Sotto le offerte si vede la quantità: ∞ per le Ricognizioni, x1 per gli oggetti. Le altre offerte e l'attacco chiudono l'NPC come prima (R-062) | Franci 05/11/2026. Nebbia: greedy 35,9% → 35,5%, probabilistico 37,5% → 38,5%, tocchi ciechi circa -1,3 |
| D-055 | Acquisti multipli e indizi di posizione | Qualsiasi acquisto lascia l'NPC aperto (anche oggetti, cure, indizi); lo chiudono attacco e "Vai via". In Nebbia la Locanda vende l'indizio di posizione (8 monete): dice in quale riga o colonna c'è un NPC estratto tra Strega, Cacciatore, Fabbro, Locanda e Mercante ancora coperti; la linea scelta riceve l'indizio col conteggio | Franci 05/11/2026. Nebbia greedy 35,5% → 38,5%, tocchi ciechi 20,2 → 17,3, turni con mossa sicura 53,9% → 59,8%. L'indizio di posizione il bot lo compra 0,07 volte a partita: effetto non misurabile col bot attuale |
| D-035 | Seed della partita | casuale a ogni partita; `MapGenerationConfig.UseFixedSeed` per riprodurre una mappa; `CurrentRunSeed` ≠ 0 vince sempre | prima ogni partita aveva la stessa mappa (Seed 12345 fisso), modifica di D-024 decisa da Athena |

## Aperte (Athena, sessione delegata 03/11/2026: implementate, da rivedere da Franci)

| ID | Tema | Decisione | Motivo |
|---|---|---|---|
| D-028 | Indizi del Mercante (R-066, D-020) | bersaglio **Boss**, 1 per 5 / 2 per 9 | il Boss è l'unico bersaglio che nessuno vende; il Mercante diventa la fonte di direzione verso l'obiettivo (P5) |
| D-029 | Posizione del Boss a parità di distanza | scelta **a caso** tra le tile alla distanza estratta | il codice v1 prendeva la prima in ordine di scansione: Boss quasi sempre sopra la partenza, prevedibile |
| D-030 | Oggetto trovato con slot occupato (R-025) | scelta **Equipaggia / Lascia** (forzieri, uccisioni); gli acquisti dagli NPC si equipaggiano subito | trovare un'arma peggiore non deve far perdere quella migliore senza scelta; l'acquisto è già una scelta |
| D-031 | Attacco base (R-023) | **1** (con il Coltello si parte con Attacco 2) | con 0 nessun mostro è battibile a inizio partita e la progressione non parte: bot Greedy 2% di vittorie; con 1 sale a 48% (bot Random 0%) |
| D-032 | Missione all'avvio (R-070) | estratta dal flusso casuale del motore (seed della partita), non dal generatore della mappa | determinismo: stesso seed = stessa missione e stessa mappa |

## Domande aperte vere

1. Nomi armi senza nome (non funzionale).
2. Boss raggiungibile con arma L5 + Anello +2 + Fabbro = 8 = Difesa Boss: da verificare col bot.
3. Livello 2 tutto negativo (R-083): accettato, da verificare col bot.
