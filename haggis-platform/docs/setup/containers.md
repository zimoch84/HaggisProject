# Kontenery Docker

## Co uruchamia docker-compose

Plik: `haggis-platform/docker-compose.yml`

- `api` - backend ASP.NET Core
- `db` - Microsoft SQL Server 2022

## Start kontenerow

Z katalogu `haggis-platform`:

```powershell
.\deploy\scripts\run.ps1 -Build
```

Szybki start bez przebudowy obrazu:

```powershell
.\deploy\scripts\run.ps1
```

## Stop kontenerow

```powershell
.\deploy\scripts\stop.ps1
```

## Adresy lokalne

- API HTTP: `http://localhost:8080`
- WebSocket: `ws://localhost:8080`
- SQL Server: `localhost,1433`

## Konfiguracja i sekrety

- Sprawdz i uzupelnij bezpiecznie:
  - `SA_PASSWORD`
  - `ConnectionStrings__Main`
- Nie commituj docelowych hasel do repozytorium.
