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

## Aperte

Nessuna.

## Domande aperte vere

1. Nomi armi senza nome (non funzionale).
2. Boss raggiungibile con arma L5 + Anello +2 + Fabbro = 8 = Difesa Boss: da verificare col bot.
3. Livello 2 tutto negativo (R-083): accettato, da verificare col bot.
