# REST API

## Cel

Backend C# udostepnia REST API jako warstwe komunikacji dla klienta Flutter.

## Zrodlo prawdy kontraktu API

- Glowne zrodlo: `OpenApiYaml/HaggisAPI.yaml`
- Kopia robocza: `haggis-platform/openapi/HaggisAPI.yaml`

Jesli jest rozjazd miedzy dokumentem i implementacja, prawda jest plik OpenAPI.

## Szybki przeglad endpointow (MVP)

| Metoda | Sciezka | Opis |
|---|---|---|
| POST | `/games` | Tworzy nowa gre |
| GET | `/games/{gameId}` | Pobiera aktualny stan gry |

## Endpointy

### POST `/games`

Tworzy nowa gre na podstawie listy graczy i opcjonalnych ustawien.

Request body (`CreateGameRequest`):

- `players` (wymagane): 2-3 graczy
- `rules.seed` (opcjonalne): seed do deterministycznego tasowania
- `rules.maxPlayers` (opcjonalne): domyslnie `3`
- `options.includeHandsInState` (opcjonalne): czy zwracac rece w stanie gry

Przyklad request:

```json
{
  "players": [
    { "id": "piotr", "displayName": "Piotr", "type": "Human" },
    { "id": "slawek", "displayName": "Slawek", "type": "AI", "aiProfile": "MonteCarlo" },
    { "id": "robert", "displayName": "Robert", "type": "AI", "aiProfile": "MonteCarlo" }
  ],
  "rules": { "seed": 12345, "maxPlayers": 3 },
  "options": { "includeHandsInState": false }
}
```

Odpowiedzi:

- `201 Created` - `CreateGameResponse` (`gameId`, `createdAt`, `state`)
- `400 Bad Request` - `ProblemDetails`

### GET `/games/{gameId}`

Pobiera stan gry.

Parametry:

- path: `gameId` (wymagany)
- query: `view` (opcjonalny): `Public` | `Player` | `Debug`, domyslnie `Public`
- query: `playerId` (wymagany tylko gdy `view=Player`)

Przyklady:

- `GET /games/{gameId}`
- `GET /games/{gameId}?view=Public`
- `GET /games/{gameId}?view=Player&playerId=piotr`
- `GET /games/{gameId}?view=Debug`

Odpowiedzi:

- `200 OK` - `GameState`
- `404 Not Found` - `ProblemDetails`
- `400 Bad Request` - `ProblemDetails` (np. `view=Player` bez `playerId`)

## Kluczowe modele (OpenAPI)

- `CreateGameRequest`
- `CreateGameResponse`
- `GameState`
- `PlayerState`
- `TurnState`
- `TableState`
- `ScoreState`
- `ProblemDetails`

## Konwencje API

- Content-Type: `application/json`
- Bledy: `application/problem+json`
- Identyfikatory: `string` (np. `gameId`, `playerId`)
- Daty: `date-time` (ISO 8601)
- Enumy domenowe: status gry, faza tury, typ gracza

## Integracja z Flutter

Flutter powinien:

- traktowac OpenAPI jako kontrakt,
- miec osobna warstwe klienta HTTP (np. repository/data source),
- mapowac `ProblemDetails` do jednego modelu bledu aplikacyjnego,
- rozdzielic modele transportowe (DTO) od modeli UI.

## TODO przed produkcja

- Dodac endpointy ruchow gracza (`play`, `pass`, akcje rundy).
- Dodac wersjonowanie API (np. `/api/v1`).
- Dodac autoryzacje i mapowanie user -> playerId.
- Dodac polityki observability (logs, metrics, tracing).

