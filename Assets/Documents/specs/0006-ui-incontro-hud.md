# 0006 — UI incontro e HUD v2

**Stato**: fatto (UI definitiva su prefab) · **Commit di riferimento**: —

## Obiettivo
Pop-up esito incontro (senza Fuggi) e HUD con HP, Attacco, monete, XP, chiavi, slot.

## Contratto (Athena)
06_presentazione §2–§4. Aggiornare `UIPopup_Encounter` (un solo pulsante), `MapGameGameplayHud`. Testi in localizzazione.

## Lato codice (Claude Code)
`[SerializeField]` per tutti i riferimenti; `Debug.LogError` + disable se nulli.

## Lato Editor (Bezi) — checklist
- Aggiornare prefab HUD e `Popup_Encounter` (rimuovere il pulsante Fuggi, aggiungere i nuovi campi).
- Palette dei 6 colori di mascheramento.

## Per Franci
- Lancia in Unity i test EditMode indicati nel report.
- Commit/push quando i test sono verdi.

## Verifica
- [ ] `Tools/RulesHarness/run.sh` verde (se tocca Rules)
- [ ] Nessuna regola nuova inventata (TODO R-xxx dove serve)
- [ ] Numeri letti da `RulesConfig`

## Report
(append-only)

### Athena — 2026-11-03 — branch `athena/v2-impl`
- `RunPrototypeUI` (IMGUI) su `GridHandler`: HUD, scelte (NPC, level up, indizi, oggetti), etichette 1–10/A–J ai bordi con il risultato degli indizi, registro eventi. Serve a giocare e bilanciare subito.
- Eccezione consapevole alla regola 10 (testi hardcoded, niente TMP): va sostituita dai prefab di questa spec. I pop-up v1 (`Popup_Encounter`, `Popup_Shop`) non vengono più aperti.

#### Per Bezi
- Prefab HUD v2 e pop-up (06 §2–§4) quando si passa dalla UI di prototipo a quella definitiva.

### Athena — 2026-11-03 (seconda parte) — UI definitiva
- Prefab `Assets/MapGame/Content/UI/Run/UIRunHUD.prefab` (Bezi), istanziato da `MapGameGameplayHudService` sotto `UIRoot.hud` al posto di `UIGameHUD`: barra superiore (HP, ATK, monete, livello/XP, chiavi, obiettivo), barra inferiore (Arma/Anello/Trinket), registro, etichette 1–10/A–J cliccabili per posare gli indizi, pannello delle scelte (riga-opzione `DecisionOption.prefab`), esito con "Continua".
- Codice: `Features/UI/Run/` (`RunUiRoot`, `RunHudView`, `DecisionPanelView`, `DecisionOptionView`, `BoardLineLabelsView`, `LineLabelView`, `RunLogView`, `EndPanelView`, `RunText`). Partita esposta da `RunSession` (`IRunSession`), testi `run.*` in `localization_master.txt` (en + it; le altre lingue in inglese).
- Portrait (D-034): orientamento Portrait, Canvas Scaler di UIRoot 1080×1920.
- L'esito resta a schermo finché il giocatore non tocca "Continua" (`ConfirmEnd`), poi la pagina risultati del template.
- La UI di prototipo IMGUI resta in `RunSession` solo come ripiego e si spegne da sola quando `UIRunHUD` è attivo.
- Lingua: il template parte in inglese (opzione salvata nel profilo); l'italiano si sceglie da Opzioni.

### Athena — 2026-11-04 — commit b1f3f33
- Verificato in Play (Unity 2022.3.62f3): menu, Opzioni, Crediti e Indietro; partita con indizi su riga/colonna, Mercante e Locanda con "Vai via", Strega con "Attacca", sconfitta, pannello esito, "Continua", pagina risultati, "Riprova" con mappa nuova.
- Corretto: il pannello esito non compariva mai. `IsAwaitingEndConfirm` veniva impostato dopo l'aggiornamento della UI, ora prima.
- Corretto: la mappa veniva generata due volte a ogni partita (avvio standalone + `GameStartedEvent`); con D-035 erano due mappe diverse. Il ripiego standalone ora parte solo se la FSM non ha uno stato attivo.
- Corretto: HP mostrati negativi a fine partita (es. -1/5): a schermo minimo 0.
- Mappa nuova a ogni partita: D-035 in `tech/08_decisioni-default.md`.

#### Modifiche di Bezi al Core del template (commit eca848c, da valutare per il 55HP Template)
- `UILocalizedText.cs`: i testi senza chiave non vengono sovrascritti; disiscrizione in `OnDestroy`.
- `UICreditsPage.cs` nuovo (pagina Crediti, Indietro = pop della navigazione) + voce Addressables.
- `Addr.cs`: rimosse le costanti `Popup_Encounter`, `Popup_Shop`, `Screens.GameplayHUD`.
- Restano nel Core: `UIGameplayHUD.cs` (HUD del template, non usato da MapGame).

#### Per Franci
- Pulsante ESCI del menu: su iOS non fa nulla e Apple lo sconsiglia; da nascondere prima di una build iOS.
- Gli sprite UI spostati da Bezi in `Assets/Art/_2D/UI/` sono esclusi da git (arte acquistata, repo pubblico): un clone pulito non li ha.

### Athena — 2026-11-04 (seconda parte) — Core autorizzato da Franci su questo branch
- `UIMainMenuPage.cs`: pulsante ESCI nascosto su iOS (`RuntimePlatform.IPhonePlayer`).
- `UIResultsPage.cs`: "PUNTEGGIO: 0" invece di "PUNTEGGIO :0"; titolo dell'esito localizzato con le chiavi nuove `game.result.won` / `game.result.lost` (prima "Hai vinto"/"Hai perso" fissi in italiano).
- Pagina risultati ridisegnata da Bezi (stile Crediti/Opzioni) e `UICamera` in `01_Menu` e `03_Results` (sparito "No cameras rendering").
- Da riportare nel 55HP Template: queste due modifiche e quelle di Bezi del commit eca848c.

### Athena — 2026-11-04 (terza parte) — rifinitura HUD (Bezi)
- `UIRunHUD`: barra superiore a "chip", barra XP con sprite del pack (prima l'Image non aveva sprite e si vedeva sempre piena), carte equipaggiamento Arma/Anello/Trinket, sfondo del registro, foglio delle scelte. `DecisionOption` e `LineLabel` (badge per il risultato degli indizi) allineati allo stesso stile.
- Verificato in Play: indizi su riga/colonna, scelta NPC con "Vai via", barra XP vuota a 0/10.

### Athena — 2026-11-04 (quarta parte) — pausa
- `PauseButtonView` (Features/UI/Run): pulsante dell'HUD che apre la pausa del template (`ISceneFlowService.GoToPauseAsync`).
- `HexGridController.TryRevealTile`: ignora i tap sul tabellone mentre la FSM è in `PauseState`.
- Core, da riportare nel 55HP Template: `UIPopup_Pause` "Esci" torna al menu invece che ai risultati (la pagina risultati leggeva l'abbandono come vittoria).
- Popup generico, pausa, toast e overlay di caricamento ridisegnati da Bezi (commit 1aa02c9).

#### Per Bezi
- In `UIRunHUD.prefab`: pulsante pausa nella barra superiore con `PauseButtonView` (`_button` assegnato).

### Athena — 2026-11-04 (quinta parte) — numeri fluttuanti
- `FloatingTextView` + `FloatingTextItem` (Features/UI/Run): danno, cura, monete e XP compaiono sopra la casella risolta, salgono e svaniscono (si fermano in pausa). Casella di riferimento: l'ultimo evento del lotto con `TileIndex`. Testi `run.float.*`.

#### Per Bezi
- Prefab `FloatingText.prefab` (TMP grande con contorno + `CanvasGroup` + `FloatingTextItem`, `_text`/`_group` assegnati).
- In `UIRunHUD.prefab`: contenitore a tutto schermo sopra il tabellone e sotto barre/foglio scelte, con `FloatingTextView` (`_container`, `_itemPrefab`); raycast disattivati.

### Athena — 2026-11-04 (sesta parte) — banner
- `RunBannerView` (Features/UI/Run): banner breve per nuovo livello e chiave ottenuta (`run.banner.*`), con comparsa a scatto e dissolvenza; più banner nello stesso lotto si accodano.

#### Per Bezi
- In `UIRunHUD.prefab`: banner a nastro centrato nella metà alta del tabellone con `CanvasGroup` + TMP grande, componente `RunBannerView` (`_group`, `_text`); sopra i numeri fluttuanti, sotto il foglio scelte; GameObject sempre attivo (la visibilità è l'alpha), raycast disattivati.

### Athena — 2026-11-04 (settima parte) — decisioni di Franci applicate
- Rules: 5 monete iniziali, partenza sicura, Boss di nuovo unico al livello 6 (D-036, D-037). Harness 50/50; bot Greedy 49,1%.
- Esito dell'attacco a un NPC mostrato prima della scelta (C1): "Perdi N HP, nessuna ricompensa" / "Vinci: <premio>", calcolato dal motore (`DecisionOption.PredictedWin/PredictedDamage`).
- Registro tolto dall'HUD (F3): `RunUiRoot` non usa più `RunLogView`.
- Tile: pallini del livello negli angoli interni (1–6, senso orario dall'alto a sinistra) e anello spesso per il livello 6 (`HexTileView._levelPips`, `_thickBorder`).
- Core, da riportare nel 55HP Template: `IHapticsService`/`HapticsService` (Light/Medium/Heavy, rispetta l'opzione Vibrazione; Android con ampiezza, iOS solo forte), registrato in `ServiceRegistry.InstallDefaults`. In MapGame: tocco non valido (leggero), danno e level up (medio), uccisione (leggero), vittoria/sconfitta (forte).

#### Per Bezi
- `UIRunHUD.prefab`: eliminare l'oggetto Log (`RunLogView`) e riassegnare lo spazio al tabellone.
- Prefab della tile: 6 pallini (SpriteRenderer) negli angoli interni in senso orario da alto-sinistra in `_levelPips`; anello esterno in `_thickBorder`.
- `HexTileConfigCatalog.DifficultyLevelBorderColors`: L1 `#9CCB5A`, L2 `#F2B544`, L3 `#5AB4F0`, L4 `#7FE0D0`, L5 `#F06A3C`, L6 `#D6244F`.
- Pagina risultati (UX 4): `RunResults` (statistiche dagli eventi, registrato a ogni partita) + `RunResultsView` sulla pagina: causa, missione, livello, Attacco, nemici e mini-boss sconfitti, personaggi, indizi, caselle, monete, danni, numero della mappa.
- Tutorial (UX 5): `TutorialPopupView`, 6 pagine `run.tutorial.*`, si apre da solo alla prima partita (PlayerPrefs `mapgame.tutorial_seen`) e dal pulsante "?".
- Mappa del giorno (LD 4): `DailyChallengeButton` nel menu → `RunSeedRequest` (seed dalla data UTC), consumato da `HexGridController` per una partita ("Riprova" dopo la mappa del giorno dà una mappa nuova).

#### Per Bezi
- `UIResults_Page.prefab`: blocco statistiche con `RunResultsView` (`_cause`, `_stats`, `_seed`); nascondere le etichette Punteggio/Record.
- `UIRunHUD.prefab`: pulsante "?" nella barra superiore e popup tutorial a pagine con `TutorialPopupView` (titolo, corpo scrollabile, "1/6", Indietro/Avanti/Chiudi), sopra tutto il resto dell'HUD.
- `MainMenu_page.prefab`: pulsante "MAPPA DEL GIORNO" (chiave `run.menu.daily`) sotto GIOCA con `DailyChallengeButton`.
