# Review kodu

Data przegladu: 2026-04-23

## Findings

### High

- `backend/tests/Haggis.Infrastructure.Tests/GameWebSocketHandlerTests.cs:159`  
  Testy infrastruktury nie kompiluja sie po zmianie konstruktora `GameWebSocketHandler`. Metoda `CreateHub()` nadal wywoluje 3-parametrowy konstruktor, a produkcyjny kod wymaga teraz `IGameWebSocketAuditLogger`. `dotnet build` i `dotnet test` dla solution padaja na `CS7036`.

- `client/Haggis.ConsoleUI.Tests/RemoteGameStateParserTests.cs:219`  
  Osobny projekt `Haggis.ConsoleUI.Tests` nie kompiluje sie. Test uzywa typu `RemoteGameState`, ktorego nie ma; parser zwraca obecnie `RemoteGameSnapshotDto`. To wyglada na rozjazd testow po refaktorze DTO.

### Medium

- `backend/src/Haggis.Infrastructure/Services/WebSocketHandlers/GameWebSocketHandler.cs:373`  
  Odbior WebSocket sklada wiadomosc do `MemoryStream` bez limitu rozmiaru. Jeden klient moze wyslac bardzo duzy frame albo fragmentowana wiadomosc i wymusic nieograniczony wzrost pamieci.

- `backend/src/Haggis.Infrastructure/Services/WebSocketHandlers/GlobalChatHub.cs:424`  
  Global chat ma analogiczny problem z nieograniczonym skladaniem wiadomosci WebSocket do pamieci.

- `backend/src/Haggis.Infrastructure/Services/WebSocketHandlers/GameWebSocketHandlingStrategies/JoinOperationStrategy.cs:16`  
  Strategie gry dereferencjonuja `Payload!` bez walidacji domenowej. `payload: null` albo puste `playerId` moga konczyc sie wyjatkiem lub zerwaniem polaczenia zamiast kontrolowanego `OperationRejected`.

- `backend/src/Haggis.Infrastructure/Services/WebSocketHandlers/GameWebSocketHandlingStrategies/CommandOperationStrategy.cs:16`  
  Strategia komend dereferencjonuje `operation.Payload!.Command!`. `command: null` powinno byc obslugiwane jako blad wejscia, nie jako niekontrolowany wyjatek.

- `backend/src/Haggis.Infrastructure/Services/Infrastructure/GameRoomStore.cs:64`  
  Backend pozwala dolaczyc dowolnej liczbie graczy do pokoju, ale silnik Haggis obsluguje tylko 2-3 graczy (`backend/src/Haggis.Infrastructure/Services/Engine/Haggis/HaggisServerGameLoop.cs:166`). Czwarty gracz moze zrobic pokoj niestartowalnym dla UI.

### Low

- `backend/src/Haggis.Infrastructure/Services/Infrastructure/GameSessionStore.cs:9`  
  Sesje sa trzymane w singletonie bez mechanizmu usuwania. Przy dluzszym dzialaniu backend bedzie akumulowal sesje dla kolejnych `gameId`.

- `backend/src/Haggis.Infrastructure/Services/Engine/Haggis/HaggisServerGameLoop.cs:189`  
  Stany gier sa trzymane w pamieci bez czyszczenia. To jest akceptowalne dla lokalnego/dev flow, ale ryzykowne dla dlugotrwalego procesu.

## Verification

- `flutter analyze` w `haggis-platform/client/Haggis.Flutter.UI`: OK.
- `flutter test` w `haggis-platform/client/Haggis.Flutter.UI`: OK, 3 testy.
- `dotnet test haggis-platform/backend/HaggisShareProject.sln --no-restore`: fail przez `CS7036` w `GameWebSocketHandlerTests.cs`.
- `dotnet test haggis-platform/client/Haggis.ConsoleUI.Tests/Haggis.ConsoleUI.Tests.csproj --no-restore`: fail przez brak typu `RemoteGameState`.

## Assumptions

- Raport tylko zapisuje wyniki review; nie naprawia znalezionych problemow.
- Raport pozostaje po polsku.
- Plik jest nowym dokumentem i nie nadpisuje istniejacego raportu.
