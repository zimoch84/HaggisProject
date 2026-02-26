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

3. Sprawdz polaczenie WebSocket:

- `ws://localhost:8080`

## Wariant B: lokalnie bez kontenerow (backend)

Uruchamiaj backend realtime z projektu `Haggis.Infrastructure`:

```powershell
dotnet run --project .\backend\src\Haggis.Infrastructure\Haggis.Infrastructure.csproj
```

Domyslny endpoint lokalny:

- `http://localhost:5555`
- `ws://localhost:5555/ws/global/chat`
- `ws://localhost:5555/ws/games/{gameId}`

## Testy silnika gry

```powershell
dotnet test .\backend\tests\Game.Engine.Tests\HaggisTests.csproj
```

## Integracja z Flutter UI

- Flutter konsumuje API WebSocket backendu.
- Bazowy URL w dev: `ws://localhost:8080`.
- Modele danych i typy wiadomosci trzymac zgodnie z AsyncAPI.
