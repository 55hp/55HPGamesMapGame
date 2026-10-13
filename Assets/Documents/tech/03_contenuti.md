# 03 — Contenuti (tile, NPC, oggetti)

Solo Athena/Franci modificano questo file. I numeri si bilanciano poi in `RulesConfig`. Per i dati in forma leggibile da macchina vedi `../data/*.csv` (devono coincidere con questo file).

## 1. Censimento del livello 1 (100 tile)

| ContentKind | Qtà | Livello | Note |
|---|---|---|---|
| Start | 1 | — | prima tile scoperta |
| Boss | 1 | 6 | Difesa 8, `IsObjective` |
| MiniBossWeapon | 1 | 5 | dà un'arma L5 |
| MiniBossSilverKey | 1 | 5 | dà la Chiave d'argento |
| MiniBossGoldKey | 1 | 5 | dà la Chiave d'oro |
| MiniBossCoins | 2 | 5 | dà 5 monete ciascuno |
| Monster1 (Piccolo 1) | 15 | 1 | Difesa 2 |
| Monster2 (Piccolo 2) | 10 | 2 | Difesa 3 |
| Trap1 | 10 | 1 | danno 1 |
| Trap2 | 5 | 2 | danno 2 |
| Bush (Arbusto) | 20 | 1 | +1 HP |
| Empty (Vuota) | 14 | — | scoperta all'inizio **[DEFAULT D-001]** |
| ChestSimpleBerries | 1 | 1 | 2 bacche (+2 HP) |
| ChestSimpleWeapon | 1 | 1 | arma L2 |
| ChestSimpleCoins | 1 | 1 | 4 monete |
| ChestSilver | 1 | 3 | arma L4, serve la Chiave d'argento **[DEFAULT D-002]** |
| ChestGold | 1 | 5 | Ammazzadraghi (L6), serve la Chiave d'oro |
| Fountain | 1 | 4 | cura totale **[D-012]** |
| Witch (Strega) | 1 | 5 | NPC |
| Hunter (Cacciatore) | 1 | 5 | NPC |
| Smith (Fabbro) | 1 | 5 | NPC |
| Inn (Locanda) | 2 | 5 | NPC |
| Trader (Mercante) | 2 | 5 | NPC |
| Farmer (Contadino) | 3 | 3 | NPC |
| Villagers (Paesani) | 3 | 3 | NPC |
| **Totale** | **100** | | |

Storia dei numeri: i Forzieri semplici passano da 5 a 3 (contenuto fisso e diverso); i Forzieri d'argento da 2 a 1 (una sola chiave d'argento); le 3 tile liberate tornano come Vuote (11 → 14), per tenere il totale a 100 **[DEFAULT D-001]**.

Tile coperte per livello (somma 85 + 14 Vuote + 1 partenza = 100):

| Livello | Tile coperte | Positive/neutre | Negative |
|---|---|---|---|
| 1 | 48 | 23 (Arbusto 20, Forziere semplice 3) = 48% | 25 (Piccolo 1 ×15, Trappola 1 ×10) = 52% |
| 2 | 15 | 0 | 15 |
| 3 | 7 | 7 | 0 |
| 4 | 1 | 1 (Fontana) | 0 |
| 5 | 13 | 8 (7 NPC L5, Forziere d'oro) = 62% | 5 (Mini-boss) = 38% |
| 6 | 1 | — | Boss |

Contando anche le 14 Vuote (già visibili) il livello 1 sarebbe 62 tile, 37 positive/neutre (60%) e 25 negative (40%): la cifra che conta per il giocatore è la prima, perché le Vuote non si cliccano.

## 2. Ricompense dei mostri

Le monete di ogni ricompensa sono aumentate dal bonus di level up (R-038).

Vedi R-036/R-037 in `02_regole.md`. Piccolo 1: 2 monete, 1 XP. Piccolo 2: 3 monete, 2 XP. Mini-boss: 3 XP più la ricompensa propria. Boss: nessuna.

## 3. NPC — premio se uccisi e offerte

Regola comune: scelta unica per tile (R-062). Combattere: Attacco ≥ 5 uccide e dà il premio. Prezzo indizio: 1 per 5 monete, 2 insieme per 9.

| NPC | Livello | Combattibile | Premio se ucciso | Offerte (a pagamento) |
|---|---|---|---|---|
| Cacciatore | 5 | sì | Arco del Cacciatore (arma L5) | Cacciagione 3 monete → +3 HP; Attrezzi del Cacciatore (Trinket) 10; indizio MiniBoss 5 / due 9 |
| Fabbro | 5 | sì | Martello del Fabbro (arma L5) | Potenziamento +1 Attacco permanente 5 (una sola volta); indizio Inn 5 / due 9 |
| Strega | 5 | sì | Bastone della Strega (arma L5) | Anello +2 Attacco 8; Lanterna (Trinket) 10; indizio Hunter 5 / due 9 |
| Locanda | 5 | no | — | Cura completa 5; indizio Trader 5 / due 9; indizio MonstersNormal (Piccolo 1 + Piccolo 2) 5 / due 9 |
| Mercante | 5 | sì | 10 monete | Arma L3 10; indizi (bersaglio e prezzi **[APERTI]**); resto del catalogo **[APERTO]** |
| Contadino | 3 | no | — | Cibo 3 monete → +1 HP; indizio Bush 5 / due **9** (come da pagina NPC) |
| Paesani | 3 | no | — | Indizio Fountain 5; indizio Smith 5; indizio Bush 5 (come da pagina NPC; nessuno sconto) |

Nessun NPC vende indizi sulla Strega, sui Forzieri, sulle Trappole, né su Contadino e Paesani. La Strega può comparire come bersaglio solo negli indizi di partenza della sua missione (`05_indizi.md`).

Gli indizi si comprano uno alla volta o in coppia; ogni indizio acquistato si usa subito (R-095), anche in catena se sono due.

## 4. Mini-boss

| Variante | Qtà | Ricompensa |
|---|---|---|
| MiniBossWeapon | 1 | arma di livello 5 (nome da definire) |
| MiniBossSilverKey | 1 | Chiave d'argento |
| MiniBossGoldKey | 1 | Chiave d'oro |
| MiniBossCoins | 2 | 5 monete ciascuno |

Tutte le varianti: Difesa 5, 3 XP, livello di mascheramento 5. Il giocatore non sa quale variante nasconde una tile di livello 5 finché non la apre (l'indizio del Cacciatore li localizza tutti e cinque senza distinguerli).

## 5. Forzieri

| Forziere | Qtà | Contenuto | Chiave |
|---|---|---|---|
| Semplice (bacche) | 1 | 2 bacche, +2 HP | no |
| Semplice (arma) | 1 | arma di livello 2 (+2 Attacco) | no |
| Semplice (monete) | 1 | 4 monete | no |
| D'argento | 1 | arma di livello 4 (+4) | Chiave d'argento |
| D'oro | 1 | Ammazzadraghi, arma di livello 6 (+6) | Chiave d'oro |

## 6. Oggetti

**Armi** (bonus Attacco = livello): Coltello L1 (dotazione), arma L2 (Forziere semplice), arma L3 (Mercante, 10 monete), arma L4 (Forziere d'argento), L5 (Arco, Martello, Bastone, arma del mini-boss), Ammazzadraghi L6 (Forziere d'oro). Nomi dei pezzi senza nome **[APERTO, non funzionale]**.

**Anello**: Anello +2 Attacco (Strega, 8 monete). **Trinket**: Attrezzi del Cacciatore (finché equipaggiati le trappole non scattano; permanenti, un eventuale limite di usi viene rimandato); Lanterna (ogni volta che il giocatore scopre una tile, ne viene **svelata la tipologia** per una adiacente a caso mai mostrata prima; la tile resta cliccabile e non è consumata; se tutte le adiacenti sono già mostrate non succede nulla).

**Consumabili immediati** (si applicano all'acquisto o alla raccolta, non occupano slot): Bacca +1 HP, Cacciagione +3 HP, Cibo +1 HP, Cura completa, Potenziamento del Fabbro.

## 7. Tile Indizio dedicata

Quantità 0 nel livello 1 **[DEFAULT D-013]**. Il tipo `ClueTile` è previsto nel modello dati (`05_indizi.md`) ma non entra nel censimento finché Franci non ne decide il numero.

## 8. Chiavi e HUD

Chiavi: solo come flag di run (HUD: due icone on/off). Non sono tile.
