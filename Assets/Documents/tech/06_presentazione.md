# 06 — Presentazione (UI e HUD)

La UI legge lo stato e gli eventi; non calcola regole. Testi dal sistema di localizzazione, non hardcoded.

## 1. Piattaforma

Mobile, portrait, pollice. Risoluzione e Canvas Scaler: **quelli già implementati** nel progetto (D-019, Franci), non si cambiano. Su schermo ~8" la griglia 10×10 deve restare leggibile: zoom e pan sulla plancia (pinch/drag) fuori scope finché non serve (spec 0006 decide se necessario dopo un test su dispositivo).

## 2. HUD

Barra superiore: HP (attuali/max), Attacco, monete, XP con barra verso il prossimo livello, due icone chiave (argento, oro; on/off). Barra inferiore: slot Arma, Anello, Trinket (icona + bonus), pulsante "Indizi".

## 3. Plancia

- Tile coperta: sagoma esagonale con **bordo colorato** per il livello di mascheramento (R-080), sei colori distinti e distinguibili anche per daltonici (aggiungere un simbolo/forma di bordo per livello). Palette concreta: asset di Bezi.
- Tile scoperta: mostra il contenuto (icona). Tile consumata: attenuata.
- Vuota e partenza: scoperte a inizio partita, non reagiscono al tap.
- Intestazioni di riga (1–10) e colonna (A–J) ai bordi; sono i punti di tocco per usare un indizio (05 §5).

## 4. Pop-up

| Pop-up | Quando | Contenuto |
|---|---|---|
| Esito incontro | dopo un click su mostro | risultato dello scambio (Attacco vs Difesa), danno, ricompensa; un solo pulsante "Continua". Nessun "Fuggi". |
| NPC | click su NPC | ritratto, elenco offerte con prezzo, pulsante "Attacca" (se combattibile), "Chiudi". Una sola scelta: dopo la scelta si chiude (R-062). Offerte non acquistabili disabilitate con motivo. |
| Level up | ogni 10 XP | tre bottoni: +1 Attacco / +1 moneta a ogni guadagno / +1 HP max. Non chiudibile senza scegliere. |
| Forziere | click su forziere | con chiave: contenuto ottenuto; senza: il forziere svanisce e il giocatore viene avvisato (R-054). |
| Indizio | quando si ottiene un indizio | testo narrativo + invito a toccare l'intestazione di una riga/colonna libera; in catena se sono due. Il pulsante HUD "Indizi" apre l'elenco di quelli già usati. |
| Fine partita | vittoria / sconfitta / Boss fuggito | titolo, motivo, riepilogo (XP, livello, monete), "Nuova partita". |

Gli esiti passano da `IUIPopupService` e chiavi Addressables esistenti (`hp55games.Addr`): riusare `Popup_Encounter` per l'esito incontro e `Popup_Shop` come base del pop-up NPC; le chiavi nuove le aggiunge Bezi.

## 5. Feedback

Danno: breve shake e numero rosso; cura: numero verde; ricompensa: numero dorato. Niente animazioni bloccanti oltre 400 ms. Audio: fuori scope.

## 6. Testi

Chiavi nel formato `ui.<area>.<nome>` e `clue.<id>` nel file di localizzazione del template. L'italiano è la lingua di default. Nomi non funzionali (nomi di armi/NPC) si possono cambiare senza toccare le regole.
