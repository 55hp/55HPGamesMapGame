# 0009 — Sistema di indizi

**Stato**: da fare · **Commit di riferimento**: —

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
- [ ] `dotnet test Tools/RulesHarness` verde (se tocca Rules)
- [ ] Nessuna regola nuova inventata (TODO R-xxx dove serve)
- [ ] Numeri letti da `RulesConfig`

## Report
(append-only)
