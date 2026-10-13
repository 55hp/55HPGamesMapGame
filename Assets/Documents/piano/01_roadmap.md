# 01 — Roadmap di preproduzione

Nessuna data: l'ordine conta, le date le decide Franci.

| Milestone | Spec | Esito verificabile |
|---|---|---|
| M0 Fondamenta | 0001 | branch `v2`, Rules compila, `dotnet test` verde |
| M1 Regole pure | 0002, 0003, 0007 | scambio/XP/level up testati; `RulesConfig` validata |
| M2 Mappa | 0004 | 1000 seed generano mappe valide; ClueCounts corretti |
| M3 Gioco giocabile | 0005, 0006, 0010, 0011 | partita completa cliccando, vittoria e sconfitta |
| M4 Contenuto sociale | 0008, 0009 | NPC e indizi funzionanti |
| M5 Pulizia e bilanciamento | 0012 | legacy rimosso; bot produce statistiche |
| M6 Playtest | — | Franci gioca su dispositivo; decisioni su D-xxx e Boss |

Dipendenze: 0002←0001; 0003←0001; 0004←0003; 0005←0002,0004; 0006←0005; 0007←0002; 0008←0003,0005; 0009←0004,0005; 0010←0003,0005; 0011←0002,0010; 0012←tutto.
Parallelo possibile: Claude Code su 0002/0003 mentre Bezi prepara palette e prefab di 0006.
