# Dokumentacja projektu Haggis

Ten katalog zawiera dokumentacj? techniczn? projektu Haggis (backend C#, AsyncAPI WebSocket, integracja z Flutter UI).

## Struktura

- `api/rest-api.md` - opis API WebSocket, konwencje i przeplyw komunikacji UI <-> backend.
- `setup/prerequisites.md` - wymagania lokalnego srodowiska.
- `setup/containers.md` - uruchamianie kontenerow Docker.
- `setup/run.md` - uruchamianie projektu lokalnie (bez kontenerow i z kontenerami).
- `decisions/decisions.txt` - indeks decyzji architektonicznych.
- `decisions/adr/` - pojedyncze rekordy decyzji (ADR).

## Aktualizacja dokumentacji

- Przy zmianie API gry: zaktualizuj `haggis-platform/openapi/HaggisAPI.yaml` i `api/rest-api.md`.
- Przy zmianie API czatu: zaktualizuj `haggis-platform/openapi/ChatAPI.yaml` i `api/rest-api.md`.
- Przy nowej decyzji technicznej: dodaj nowy plik w `decisions/adr/` i dopisz wpis do `decisions/decisions.txt`.
- Przy zmianie deploymentu/uruchamiania: zaktualizuj `setup/containers.md` oraz `setup/run.md`.
