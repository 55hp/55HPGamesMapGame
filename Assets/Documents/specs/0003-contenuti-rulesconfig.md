# 0003 — Modello contenuti e RulesConfig

**Stato**: da fare · **Commit di riferimento**: —

## Obiettivo
Modello dati puro dei contenuti e config dei numeri di regola, con ScriptableObject che la espone.

## Contratto (Athena)
`ContentKind` (enum), `ContentDef` (livello di mascheramento, Difesa, danno, ricompense), `ItemDef`, `NpcDef` + offerte, `RulesConfig` (tutti i numeri di `02`/`03`, censimento). `RulesConfigAsset` (ScriptableObject in Features) che costruisce `RulesConfig`.

## Lato codice (Claude Code)
`RulesConfig` immutabile e validabile (`Validate()` → somma censimento = W×H). Test: i dati di default coincidono con `data/*.csv`.

## Lato Editor (Bezi) — checklist
- Creare l'asset `RulesConfig_Level1.asset` (menu di creazione fornito dallo script) e assegnarlo a `GameBootstrap`.
- Assegnare icone/sprite ai contenuti (dopo).

## Per Franci
- Lancia in Unity i test EditMode indicati nel report.
- Commit/push quando i test sono verdi.

## Verifica
- [ ] `dotnet test Tools/RulesHarness` verde (se tocca Rules)
- [ ] Nessuna regola nuova inventata (TODO R-xxx dove serve)
- [ ] Numeri letti da `RulesConfig`

## Report
(append-only)
