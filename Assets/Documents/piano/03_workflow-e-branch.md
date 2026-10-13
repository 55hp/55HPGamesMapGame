# 03 — Workflow e branch

## Branch

```
git clone https://github.com/55hp/55HPGamesMapGame.git
cd 55HPGamesMapGame
git checkout -b v2 fdfe080fbf927808e80481022913526f5b35384e
```

`develop` non si tocca. Si lavora su `v2`. **Mai merge, da nessuna parte**; push solo su conferma di Franci.

## Ciclo di un task

1. Franci sceglie la spec (o chiede ad Athena di scriverne una).
2. Athena scrive intestazione, obiettivo e contratto; produce il prompt per Claude Code (e per Bezi se serve).
3. Claude Code implementa, esegue `dotnet test Tools/RulesHarness`, compila "Lato codice", scrive report con commit.
4. Bezi esegue la checklist Editor dopo il commit di Claude Code e scrive il report.
5. Franci lancia i test Unity, fa playtest, committa/pusha.

## Prompt-tipo (inglese, terso)

Claude Code:
```
Read CLAUDE.md, Assets/Documents/tech/00_INDICE.md, then specs/NNNN-*.md. Implement "Lato codice" only. Follow [DEFAULT] items as written; for rule gaps leave // TODO R-xxx and add a question under Report > Domande. Run dotnet test Tools/RulesHarness. Commit locally, files by path, no merge, no push. Fill the Bezi checklist and "Per Franci".
```
Bezi:
```
Read Assets/Documents/Bezi_Rules.md and specs/NNNN-*.md. Do the "Lato Editor (Bezi)" checklist at the commit named in the spec. No C#. Append your report to the spec.
```

## Convenzioni

Commit: `feat(rules): …`, `docs: …`, `chore: …`. Un commit per spec coerente. Messaggio in inglese, descrizione breve.
