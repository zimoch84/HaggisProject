# Dokumentacja projektu Haggis

Ten katalog zawiera dokumentację techniczną projektu Haggis (backend C#, REST API, integracja z Flutter UI).

## Struktura

- `api/rest-api.md` - opis API REST, konwencje i przeplyw komunikacji UI <-> backend.
- `setup/prerequisites.md` - wymagania lokalnego srodowiska.
- `setup/containers.md` - uruchamianie kontenerow Docker.
- `setup/run.md` - uruchamianie projektu lokalnie (bez kontenerow i z kontenerami).
- `decisions/decisions.txt` - indeks decyzji architektonicznych.
- `decisions/adr/` - pojedyncze rekordy decyzji (ADR).

## Aktualizacja dokumentacji

- Przy zmianie API: zaktualizuj `OpenApiYaml/HaggisAPI.yaml` i `api/rest-api.md`.
- Przy nowej decyzji technicznej: dodaj nowy plik w `decisions/adr/` i dopisz wpis do `decisions/decisions.txt`.
- Przy zmianie deploymentu/uruchamiania: zaktualizuj `setup/containers.md` oraz `setup/run.md`.
