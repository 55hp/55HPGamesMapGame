# 0006 — UI incontro e HUD v2

**Stato**: da fare · **Commit di riferimento**: —

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
- [ ] `dotnet test Tools/RulesHarness` verde (se tocca Rules)
- [ ] Nessuna regola nuova inventata (TODO R-xxx dove serve)
- [ ] Numeri letti da `RulesConfig`

## Report
(append-only)
