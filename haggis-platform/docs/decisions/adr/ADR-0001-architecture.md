# ADR-0001: Architektura backend C# + AsyncAPI WebSocket + Flutter UI

- ID: `ADR-0001`
- Status: Accepted
- Data: 2026-02-09

## Context

Projekt Haggis potrzebuje:

- backendu z logika gry,
- stabilnego kontraktu komunikacji z UI,
- klienta mobilnego/webowego pisanego we Flutter.

Istnieje juz znaczaca czesc silnika gry w `backend/src/Game.Engine`.

## Decision

Przyjmujemy architekture:

- backend: C#/.NET (ASP.NET Core),
- kontrakt komunikacji: AsyncAPI nad WebSocket,
- klient: Flutter konsumujacy wiadomosci WebSocket,
- uruchomienie deweloperskie: Docker Compose (API + SQL Server).

## Consequences

- Plusy:
  - Jedno stale polaczenie i latwe pushowanie zmian stanu gry.
  - Jasny kontrakt komunikatow niezalezny od UI.
  - Mozliwosc rownoleglego rozwoju backendu i Fluttera.
- Minusy:
  - Koniecznosc utrzymania mapowania modeli backend <-> DTO wiadomosci <-> modele Flutter.
  - Potrzeba obslugi reconnect, heartbeat i kolejnosci wiadomosci.
- Ryzyka:
  - Rozjazd implementacji backendu i specyfikacji AsyncAPI.
  - Niespojnosc konfiguracji srodowisk lokalnych.

## Alternatives considered

1. Monolit bez formalnego kontraktu API.
2. REST zamiast WebSocket.
3. gRPC zamiast WebSocket.
4. UI webowe zamiast Flutter.

## Follow-up

- Dodac proces walidacji AsyncAPI w CI.
- Ustalic standard wersjonowania wiadomosci.
- Dopisac ADR dla strategii autoryzacji i uwierzytelniania polaczen.
