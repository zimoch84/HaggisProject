# Dokumentacja projektu Haggis

Ten katalog zawiera dokumentacj? techniczn? projektu Haggis (backend C#, AsyncAPI WebSocket, integracja z Flutter UI).

## Struktura

- `api/rest-api.md` - opis API WebSocket, konwencje i przeplyw komunikacji UI <-> backend.
- `api/post-login-flow.md` - flow sesji gracza po zalogowaniu: global chat, pokoj, start gry.
- `postman/websocket-messages.md` - gotowe komunikaty WebSocket do testow w Postmanie (request/response).
- `setup/prerequisites.md` - wymagania lokalnego srodowiska.
- `setup/containers.md` - uruchamianie kontenerow Docker.
- `setup/run.md` - uruchamianie projektu lokalnie (bez kontenerow i z kontenerami).
- `decisions/decisions.txt` - indeks decyzji architektonicznych.
- `decisions/adr/` - pojedyncze rekordy decyzji (ADR).

## Aktualizacja dokumentacji

- Kontrakty API (jedno zrodlo prawdy): `haggis-platform/asyncApi/*.yaml`.
- Przy zmianie API gry/czatu/serwera: zaktualizuj odpowiedni plik w `asyncApi/` i `api/rest-api.md`.
- Przy nowej decyzji technicznej: dodaj nowy plik w `decisions/adr/` i dopisz wpis do `decisions/decisions.txt`.
- Przy zmianie deploymentu/uruchamiania: zaktualizuj `setup/containers.md` oraz `setup/run.md`.
