# Uruchomienie projektu

## Wariant A: uruchomienie przez Docker (zalecane na start)

1. Przejdz do katalogu:

```powershell
cd haggis-platform
```

2. Uruchom kontenery:

```powershell
.\deploy\scripts\run.ps1 -Build
```

3. Sprawdz API:

- `http://localhost:8080/swagger`

## Wariant B: lokalnie bez kontenerow (backend)

Aktualnie katalog `backend/src/Game.Api` jest przygotowany pod API, ale nie ma jeszcze kompletnego projektu `.csproj` do uruchomienia.

Docelowy proces lokalnego startu backendu:

1. Przygotuj SQL Server (lokalnie lub kontener).
2. Ustaw `ConnectionStrings__Main` dla projektu API.
3. Uruchom API komenda `dotnet run --project <sciezka-do-Game.Api.csproj>` po dodaniu projektu API.

## Testy silnika gry

```powershell
dotnet test .\backend\tests\Game.Engine.Tests\HaggisTests.csproj
```

## Integracja z Flutter UI

- Flutter konsumuje REST API backendu.
- Bazowy URL w dev: `http://localhost:8080`.
- Modele danych i endpointy trzymac zgodnie z OpenAPI.
