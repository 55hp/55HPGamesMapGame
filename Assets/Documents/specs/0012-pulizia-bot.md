# 0012 — Pulizia legacy e bot di bilanciamento

**Stato**: fatto (bot e pulizia) · **Commit di riferimento**: —

## Obiettivo
Rimuovere il legacy e produrre il bot di simulazione.

## Contratto (Athena)
Architettura §7, 07 §3. Rimuovere `MapClusterGenerator`, Food*, analyzer a cluster; `RandomPolicy` e `GreedyPolicy`, CSV di output.

## Lato codice (Claude Code)
Eseguibile via `dotnet test` o console in `Tools/`. Output in `Tools/RulesHarness/out/` (ignorato da git).

## Lato Editor (Bezi) — checklist
- Scollegare gli asset legacy (MapGenerationConfig cluster, LevelConfig_Test_1) dalle scene prima della cancellazione.

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
- `RandomPolicy`, `GreedyPolicy`, `BalanceSimulator` in Rules/Bot. Esecuzione: `Tools/RulesHarness/run.sh --sim 10000 [file.csv] [Nome=valore …]`.
- Risultati 10 000 partite (D-031 applicata): Greedy 48,2% vittorie, Random 0%. Con Attacco base 0 (regole originali) Greedy 2,1%. HP 6 → 62%, HP 7 → 78% (non applicato: HP 5 è una decisione chiusa).
- Pulizia legacy (MapClusterGenerator, Food, analyzer a cluster, popup v1) non ancora fatta.

### Athena — 2026-11-03 (seconda parte) — pulizia legacy
- Rimossi (codice): `MapClusterGenerator`, gli analyzer a cluster/strade/reachability, `UIPopup_Encounter`, `UIPopup_Shop`, `RevealEffectPopupDispatcher`, `MapGameGameplayHud`, `SurvivalConfig`, `TraderConfig`, `EnemyConfig`, `EncounterEvents`, `TradeEvents`, gli stub v1 di `HexGridController`.
- Scollegati e rimossi da Bezi prima del codice: dispatcher dalla scena, voci Addressables e prefab dei due popup, `UIGameHUD`, i tre config dal `MainConfigCatalog`.
- Lasciati di proposito (inerti, non toccano la partita): `LevelConfig`, `EventClusterShape`/`Catalog` e i loro editor, i campi cluster di `MapGenerationConfig`, `Food`/`HasKeyDL6` in `IGameContextService` (template Core, non modificabile).

### Bezi — 2026-11-03 — commit ae412b99
- Lavoro non committato sopra ae412b99 (branch athena/v2-impl). Nessun file C# toccato.
- Scena Assets/Scenes/02_Gameplay.unity: rimosso il componente RevealEffectPopupDispatcher e il suo GameObject /RevealEffectPopupDispatcher (restava solo il Transform, nessun figlio).
- Addressables, gruppo UI: rimosse le voci content/ui/popups/encounter, content/ui/popups/shop e content/ui/screens/gameplay_hud.
- Cancellati, con i .meta, dopo una scansione che non ha trovato riferimenti in scene, prefab o asset: Assets/Content/UI/Popups/UIPopup_Encounter.prefab, Assets/Content/UI/Popups/UIPopup_Shop.prefab, Assets/Content/UI/HUD/UIGameHUD.prefab, Assets/MapGame/Content/Configs/SurvivalConfig_Test.asset, TraderConfig.asset, EnemyConfig.asset.
- Assets/Content/Configs/MainConfigCatalog.asset: ora contiene solo LevelConfig_Test_1 e MapGenerationConfig_Test. HexTileConfigCatalog non era nel catalogo.
- Verificato su disco: nessuna scena, prefab o asset contiene componenti degli script rimossi (MapClusterGenerator, UIPopup_Encounter, UIPopup_Shop, RevealEffectPopupDispatcher, MapGameGameplayHud, SurvivalConfig, TraderConfig, EnemyConfig). Console senza errori dopo la ricompilazione.
- SerializeField: MapGameGameplayHudService._hudPrefab in 02_Gameplay e' assegnato a Assets/MapGame/Content/UI/Run/UIRunHUD.prefab. Nessun campo lasciato vuoto tra quelli controllati.
- Per Claude Code: Addr.cs (Core) contiene ancora Popup_Encounter, Popup_Shop e GameplayHUD, che puntano a indirizzi non piu' esistenti. Commenti residui che citano UIPopup_Encounter/UIPopup_Shop in RevealEffect.cs, TileTypePalette.cs e MapGenerationConfigEditor.cs. Il messaggio d'errore in MapGameGameplayHudService cita ancora UIGameHUD.
- Checklist "Lato Editor": LevelConfig_Test_1 e MapGenerationConfig non sono stati scollegati, per istruzione esplicita di Franci e in linea con il report di Athena (lasciati di proposito). Da confermare se il punto della checklist e' superato.
- Fuori spec, richiesto da Franci nello stesso task: UIRunHUD (sprite dialog_box_0 Sliced e colore scuro su Decision/Sheet, TopBar, BottomBar; slot del BottomBar a larghezza uguale con LayoutElement; testi Value con Auto Size 20-30) e DecisionOption (Button in Color Tint).
- Rifinitura UI successiva: Auto Size su DecisionOption Label (28-42) e Detail (22-30), con raycastTarget disattivato su entrambi; Auto Size su UIRunHUD TopBar/GoalText (20-28) e End/Overlay/Box/Summary (24-36). Motivo: le stringhe italiane piu' lunghe di localization_master.txt uscivano dai contenitori a dimensione fissa. I bordi 9-slice degli sprite ui_pack_elements risultano gia' impostati nei .meta e coerenti con il disegno.

