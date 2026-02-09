# ADR-0001: Architektura backend C# + REST API + Flutter UI

- ID: `ADR-0001`
- Status: Accepted
- Data: 2026-02-09

## Context

Projekt Haggis potrzebuje:

- backendu z logika gry,
- stabilnego API do komunikacji z UI,
- klienta mobilnego/webowego pisanego we Flutter.

Istnieje juz znaczaca czesc silnika gry w `backend/src/Game.Engine`.

## Decision

Przyjmujemy architekture:

- backend: C#/.NET (ASP.NET Core),
- kontrakt komunikacji: REST API opisane OpenAPI,
- klient: Flutter konsumujacy REST,
- uruchomienie deweloperskie: Docker Compose (API + SQL Server).

## Consequences

- Plusy:
  - Jasny kontrakt API niezalezny od UI.
  - Mozliwosc rownoleglego rozwoju backendu i Fluttera.
  - Dobre wsparcie narzedziowe .NET i Flutter.
- Minusy:
  - Koniecznosc utrzymania mapowania modeli backend <-> DTO API <-> modele Flutter.
  - Potencjalny narzut serializacji i komunikacji HTTP.
- Ryzyka:
  - Rozjazd implementacji backendu i specyfikacji OpenAPI.
  - Niespojnosc konfiguracji srodowisk lokalnych.

## Alternatives considered

1. Monolit bez formalnego kontraktu API.
2. gRPC zamiast REST.
3. UI webowe zamiast Flutter.

## Follow-up

- Dodac proces walidacji OpenAPI w CI.
- Ustalic standard wersjonowania endpointow.
- Dopisac ADR dla strategii autoryzacji.
