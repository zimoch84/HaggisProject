# WebSocket API

## Cel

Backend C# udostepnia API WebSocket jako warstwe komunikacji dla klienta Flutter.

## Zrodlo prawdy kontraktu API

- Gra (glowne zrodlo): `haggis-platform/openapi/HaggisAPI.yaml`
- Gra (kopia robocza): `haggis-platform/docs/api/HaggisAPI.yaml`
- Czat (glowne zrodlo): `haggis-platform/openapi/ChatAPI.yaml`
- Czat (kopia robocza): `haggis-platform/docs/api/ChatAPI.yaml`

Jesli jest rozjazd miedzy dokumentem i implementacja, prawda sa pliki AsyncAPI.

## Szybki przeglad kanalow (MVP)

| Kierunek | Kanal | Opis |
|---|---|---|
| client -> server | `games/create` | Tworzenie nowej gry |
| client -> server | `games/{gameId}/actions` | Ruch gracza (`Play`/`Pass`) |
| server -> client | `games/create` | Wynik tworzenia gry |
| server -> client | `games/{gameId}/actions` | `PlayerAction` (AI lub Human), wynik akcji i nowy stan |
| server -> client | `games/{gameId}/state` | Strumien aktualizacji stanu |
| client -> server | `chat/global` | Wyslanie wiadomosci czatu |
| server -> client | `chat/global` | Strumien wiadomosci czatu |

## Wiadomosci

### CreateGameRequest

Wysylana przez klienta na kanal `games/create`.

Pola:

- `players` (wymagane): 2-3 graczy
- `rules.seed` (opcjonalne): seed do deterministycznego tasowania
- `rules.maxPlayers` (opcjonalne): domyslnie `3`
- `options.includeHandsInState` (opcjonalne): czy zwracac rece w stanie gry

### CreateGameResponse

Wysylana przez serwer na kanal `games/create` po utworzeniu gry.

- `gameId`
- `createdAt`
- `state` (`GameState`)

### PlayerAction

Wysylana przez klienta na kanal `games/{gameId}/actions`, a przez serwer moze byc odeslana jako wykonana akcja (AI lub Human).

- `type`: `Play` | `Pass`
- `playerId`
- `cards` (wymagane dla `Play`)

### GameState

Wysylana przez serwer na `games/{gameId}/actions` i `games/{gameId}/state`.

### ProblemDetails

Wysylana przez serwer przy bledzie walidacji albo domenowym.

### SendChatMessageRequest

Wysylana przez klienta na kanal `chat/global`.

- `playerId`
- `text` (1-500 znakow)

### ChatMessage

Wysylana przez serwer na kanal `chat/global`.

- `messageId`
- `playerId`
- `text`
- `createdAt`

## Konwencje API

- Format wiadomosci: `application/json`
- Transport: `ws://` (lokalnie `ws://localhost:8080`)
- Bledy: `ProblemDetails`
- Identyfikatory: `string` (`gameId`, `playerId`)
- Daty: `date-time` (ISO 8601)
- Enumy domenowe: status gry, faza tury, typ gracza

## Integracja z Flutter

Flutter powinien:

- traktowac AsyncAPI jako kontrakt,
- miec osobna warstwe klienta WebSocket,
- mapowac `ProblemDetails` do jednego modelu bledu aplikacyjnego,
- rozdzielic modele transportowe (DTO) od modeli UI.

## TODO przed produkcja

- Dodac autoryzacje polaczen i mapowanie user -> playerId.
- Dodac heartbeat/reconnect oraz idempotencje dla komend klienta.
- Dodac wersjonowanie kontraktu wiadomosci.
- Dodac polityki observability (logs, metrics, tracing).

