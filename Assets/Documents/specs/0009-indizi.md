# 0009 — Sistema di indizi

**Stato**: fatto (regole e UI di prototipo) · **Commit di riferimento**: —

## Obiettivo
ClueService, pannello indizi, etichette di riga/colonna.

## Contratto (Athena)
05_indizi. Uso immediato (R-095): coda `PendingClueChoices`, catena di 2, indizio perso se nessuno slot libero; `ClueService.Use` con conteggi dal layout; UI: invito a toccare l'intestazione di riga/colonna e pannello di sola lettura.

## Lato codice (Claude Code)
Test come in 07 §2. Nessuna logica nella UI.

## Lato Editor (Bezi) — checklist
- Prefab `Panel_Clues` (sola lettura), etichette di bordo (simbolo + numero), icone dei ClueTarget.

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
- Coda di decisioni `ClueLine` con le sole linee libere; catena di 2; indizio perso se non c'è una linea libera (R-096); conteggi dal layout (R-093). Etichette di bordo nella UI di prototipo.
