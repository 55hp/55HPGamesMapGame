# CLAUDE.md

MapGame v2 — puzzle-roguelike a griglia esagonale 10×10, mobile (Unity 2022.3.62f3 + URP), costruito sul "55HP Games Unity Mobile Template". Branch di lavoro: `v2`.

Documentazione: `Assets/Documents/README.md` ("il README"), regole in `Assets/Documents/tech/02_regole.md` e `03_contenuti.md`, indice in `tech/00_INDICE.md`. **Fonte di verità: il repo.** Notion non fa parte del tuo flusso: non leggerlo, non citarlo.

Questo file contiene solo ciò che cambia *come* lavori.

## Comandi

- Test delle regole fuori da Unity: `dotnet test Tools/RulesHarness`. Eseguilo sempre prima di dichiarare finito un task che tocca `Assets/Game/Rules/` o i suoi test.
- Aprire: Unity Hub, Editor `2022.3.62f3` esatto (come `ProjectSettings/ProjectVersion.txt`).
- Test in Unity: Test Runner → EditMode (`Assets/Tests/EditMode/`). Li lancia Franci; tu indica quali.
- `.sln`/`.csproj` alla radice sono generati da Unity: non modificarli. L'unico progetto .NET che possiedi è `Tools/RulesHarness/`.

## L'Editor di Franci è quasi sempre aperto

- Mai lanciare Unity da CLI (`-batchmode`, `-executeMethod`).
- Un `.cs` nuovo non ha `.meta` finché Franci non torna sull'Editor. Non scrivere a mano `.meta` né `.asset`/`.prefab`/`.unity`.

## Ownership

| Attore | Possiede | Non tocca |
|---|---|---|
| Claude Code | C#, test, asmdef, `Tools/RulesHarness/`, sezione Stato del README, "Lato codice" delle spec, questo file | scene, prefab, `.asset`, materiali, report di Bezi, `tech/*` |
| Bezi | scene, prefab, Inspector, asset, layout UI | C# |
| Franci | design, priorità, commit/push finali, playtest, bilanciamento | — |
| Athena | `tech/`, intestazione delle spec, prompt | codice e asset |

Il confine codice/scena è di Bezi: se uno script ha bisogno di un oggetto di scena/prefab/asset, dichiara un `[SerializeField]` e scrivi il setup mancante nella checklist della spec. Nel report distingui **per Bezi** (wiring, gerarchie, prefab, asset) e **per Franci** (una singola modifica isolata).

### Spec e report

- Spec: `Assets/Documents/specs/NNNN-slug.md`. Athena scrive intestazione, obiettivo, contratto; tu compili "Lato codice", checklist Bezi, "Per Franci" con lo stato reale e il commit.
- Report in coda sotto `## Report`, append-only. Intestazione: `### Claude Code — AAAA-MM-GG — commit abc1234`. Non modifichi mai le voci di Bezi o Franci.
- Domande sulle regole: se `02`/`03` non coprono un caso non inventare. Implementa il resto, lascia `// TODO R-xxx: <domanda>` nel punto esatto e scrivi la domanda sotto `#### Domande`. Le voci **[DEFAULT]** sono decisioni valide: implementale come scritte.
- Se un report di Bezi contraddice il disco, è vecchio: controlla il commit che dichiara.

## Patch di documentazione da Athena

I file in `tech/` sono di Athena. Li modifichi solo se un prompt ti dà il testo esatto: applicalo parola per parola. Se la riga indicata è diversa, fermati e riportalo. Le patch vanno in un commit separato "docs: patch Athena (...)" con `git diff --stat`. Se il codice contraddice un documento, non toccare il documento: domanda nel report.

## Regole che cambiano cosa scrivi

1. **Assembly Rules puro**: `hp55games.MapGame.Rules` ha `noEngineReferences: true`. Niente `UnityEngine`, `System.Random`, `DateTime.Now`, I/O. Casualità solo da `IRandomSource`. Deterministico.
2. **C# 9 al massimo**, API .NET Standard 2.1: niente file-scoped namespace, `global using`, `record struct`, `required`, raw string literal. `init` ammesso.
3. **La presentazione non modifica lo stato di regole**: legge `RunState` e gli eventi; agisce solo chiamando i servizi di Rules.
4. **Ogni decisione espone le sue opzioni legali.**
5. **Numeri di regola solo in `RulesConfig`**: nessuna costante di gioco negli effetti, nella UI o nei test (i test leggono da `RulesConfig`).
6. **Niente collegamenti a runtime di oggetti autorabili**: niente `AddComponent`, `FindObjectOfType`, `GameObject.Find`, `transform.Find`, `Camera.main`. Eccezione: oggetti generati dai dati (celle, tile), istanziati da prefab assegnati in Inspector.
7. **Riferimento nullo**: `Debug.LogError` con cosa manca e dove, poi il componente si disabilita. Non ricostruire da codice layout o valori del prefab.
8. **Una sola fonte di verità per valore**: prefab/scena **oppure** asset di config. Il codice non sovrascrive valori autorati.
9. **Niente singleton né statico mutabile**: si usa il `ServiceRegistry` del template. Composition root: `GameBootstrap`.
10. **Testi per il giocatore**: italiano, nel sistema di localizzazione del template; mai hardcoded; sempre TextMeshPro.
11. **Input**: Legacy Input Manager / quello già configurato nel template (non cambiarlo).
12. **Commit locali; push solo su conferma di Franci; MAI merge, da nessuna parte.** `git add` per percorso, mai `git add -A`.
13. Nessun riferimento a file che non esistono nel repo.
14. **Codice legacy da rimuovere** (`MapClusterGenerator`, Food, Flee, analyzer a cluster): non estenderlo né correggerlo; la rimozione è la spec 0012. Se ti blocca prima, segnalalo.
15. **Template Core** (`hp55games.Mobile.Core.*`): non modificarlo. Gli eventi nuovi vivono in Features.
16. Namespace = nome dell'assembly (`hp55games.MapGame.Rules`, `hp55games.MapGame.Features.*`).

## Trappole note

- La griglia di default dell'asset legacy non è 10×10: la mappa v2 viene dal censimento in `RulesConfig` (`tech/04_mappa.md`).
- `TileType` (int, 0–21): non rinumerare mai; estendi in coda.
- Il vecchio CLAUDE.md citava `AestheticClusterMapGenerator` e un costo Chebyshev del cibo: obsoleto.

## Lingua

Documentazione e commenti in italiano; identificatori del codice in inglese.
