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
