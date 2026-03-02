# Haggis.Infrastructure Class Overview

Ten dokument opisuje odpowiedzialność typów w projekcie `Haggis.Infrastructure`, ich cel oraz typowe użycie. Ma służyć jako mapa przed refaktorem, szczególnie tam, gdzie dziś mieszają się transport, orkiestracja i logika specyficzna dla gry.

## Ogólny przepływ

1. `Program` składa aplikację i rejestruje zależności.
2. `ChatWebSocketHandler` i `GameWebSocketHub` przyjmują połączenia WebSocket.
3. `GameWebSocketHub` zamienia wiadomości klienta na `GameClientMessage`.
4. `GameCommandApplicationService` orkiestruje wykonanie komendy na sesji.
5. `GameSession` utrzymuje bieżący snapshot i numer kolejności.
6. `IGameEngine` i jego implementacja `HaggisGameEngine` wyliczają kolejny stan.
7. `HaggisServerGameLoop` tłumaczy generyczne komendy transportowe na działania domenowe Haggisa.
8. Wynik wraca jako `GameEventMessage`.

## Composition Root

### `Program`
Plik: `Program.cs`

Cel:
- uruchamia aplikację ASP.NET,
- rejestruje wszystkie zależności w DI,
- wystawia endpointy WebSocket dla globalnego czatu i gry.

Użycie:
- to jest punkt wejścia procesu,
- nie zawiera logiki biznesowej, tylko składanie obiektów i mapowanie endpointów.

Dlaczego istnieje:
- centralizuje konfigurację hosta i zależności,
- pokazuje realną architekturę runtime lepiej niż same interfejsy.

## Transport: WebSocket i wejście z klienta

### `ChatWebSocketHandler`
Plik: `Services/ChatWebSocketHandler.cs`

Cel:
- cienki adapter HTTP -> WebSocket dla globalnego czatu.

Użycie:
- sprawdza, czy żądanie jest WebSocketem,
- akceptuje połączenie,
- przekazuje obsługę do `GlobalChatHub`.

Dlaczego istnieje:
- oddziela integrację z `HttpContext` od właściwego huba czatu,
- pozwala utrzymać `GlobalChatHub` jako klasę aplikacyjną, a nie endpoint.

### `GlobalChatHub`
Plik: `Services/GlobalChatHub.cs`

Cel:
- obsługuje globalny kanał czatu,
- przyjmuje operacje `chat`, `listroom`, `createroom`, `privatechat`,
- utrzymuje listę aktywnych klientów globalnego czatu.

Użycie:
- bootstrapuje klienta historią i listą kanałów,
- publikuje wiadomości czatu,
- tworzy pokoje gier lub prywatne pokoje przez `IGameRoomStore`,
- wiąże socket z graczem przez `IPlayerSocketRegistry`.

Dlaczego istnieje:
- to osobny transport i osobny model komunikacji niż gra,
- pełni rolę lobby/discovery dla pokojów gry.

Uwagi projektowe:
- ma sporo ręcznego parsowania JSON,
- miesza transport, walidację wejścia i część use-case'ów związanych z pokojami.

### `GameWebSocketHub`
Plik: `Services/Hubs/GameWebSocketHub.cs`

Cel:
- obsługuje WebSocket per gra (`/ws/games/{gameId}`),
- przyjmuje operacje `join`, `create`, `chat`, `command`,
- rozsyła zdarzenia gry do wszystkich klientów podłączonych do danego `gameId`.

Użycie:
- dla `command` deleguje do `IGameCommandApplicationService`,
- dla `create` tworzy pokój i od razu wysyła komendę `Initialize`,
- dla `join` dopisuje gracza do pokoju,
- dla `chat` nadaje czat lokalny w obrębie gry.

Dlaczego istnieje:
- jest główną bramą wejściową dla runtime gry,
- oddziela protokół WebSocket od silnika i sesji.

Uwagi projektowe:
- to jest dziś miejsce, w którym widać transportowy charakter `GameCommand`,
- zawiera sporo branchingu po stringowym `operation`.

## Application Layer

### `IGameCommandApplicationService`
Plik: `Services/Application/IGameCommandApplicationService.cs`

Cel:
- kontrakt use-case'a wykonania komendy gry.

Użycie:
- wywoływany przez `GameWebSocketHub`,
- zwraca gotową wiadomość wyjściową `GameEventMessage`.

Dlaczego istnieje:
- odcina transport od szczegółów sesji i silnika,
- daje jedno wejście dla wykonania komendy.

### `GameCommandApplicationService`
Plik: `Services/Application/GameCommandApplicationService.cs`

Cel:
- orkiestruje wykonanie `GameClientMessage` na konkretnej sesji gry,
- zamienia wyjątki domenowe/aplikacyjne na `CommandRejected`.

Użycie:
- pobiera sesję z `IGameSessionStore`,
- przy `Initialize` może wzbogacić payload o listę graczy z pokoju,
- po wykonaniu zwraca `CommandApplied` z nowym stanem.

Dlaczego istnieje:
- centralizuje logikę use-case'a,
- nie pozwala, żeby hub bezpośrednio operował na sesjach i engine.

Uwagi projektowe:
- zna strukturę `GameCommand.Payload`,
- ma przez to częściową wiedzę o transporcie i kontrakcie wejściowym.

## Session Layer

### `IGameSession`
Plik: `Services/Interfaces/IGameSession.cs`

Cel:
- kontrakt jednostki przechowującej stan jednej gry.

Użycie:
- udostępnia `CurrentState`, `OrderPointer` i `Apply`.

Dlaczego istnieje:
- pozwala ukryć synchronizację i storage za prostym API.

### `GameSession`
Plik: `Services/Infrastructure/Sessions/GameSession.cs`

Cel:
- przechowuje bieżący stan konkretnej gry,
- serializuje dostęp do modyfikacji przez `lock`,
- utrzymuje monotoniczny `OrderPointer`.

Użycie:
- `Apply` bierze `message.State` albo `CurrentState` jako stan bazowy,
- deleguje wyliczenie następnego stanu do `IGameEngine`,
- zapisuje wynik jako nowy stan sesji.

Dlaczego istnieje:
- to granica spójności dla jednej gry,
- gwarantuje sekwencyjne nakładanie komend.

### `IGameSessionStore`
Plik: `Services/Interfaces/IGameSessionStore.cs`

Cel:
- kontrakt repozytorium sesji gry.

Użycie:
- `GameCommandApplicationService` pobiera przez niego sesję dla `gameId`.

### `GameSessionStore`
Plik: `Services/Infrastructure/Sessions/GameSessionStore.cs`

Cel:
- in-memory storage sesji gry.

Użycie:
- tworzy `GameSession` leniwie przy pierwszym użyciu `gameId`.

Dlaczego istnieje:
- oddziela lifecycle sesji od application service,
- w przyszłości może zostać podmieniony na storage trwały lub rozproszony.

## Engine Abstraction

### `IGameEngine`
Plik: `Services/Interfaces/IGameEngine.cs`

Cel:
- abstrakcja silnika stanu gry niezależna od konkretnej gry.

Użycie:
- `GameSession` wywołuje `CreateInitialState` oraz `SimulateNext`.

Dlaczego istnieje:
- pozwala utrzymać sesję niezależną od szczegółów Haggisa.

### `HaggisGameEngine`
Plik: `Services/Engine/HaggisGameEngine.cs`

Cel:
- adapter między generycznym `IGameEngine` a domeną Haggisa,
- buduje snapshot JSON zwracany do klientów.

Użycie:
- deleguje wykonanie ruchu do `HaggisServerGameLoop`,
- automatycznie rozgrywa ruchy AI aż do tury człowieka, końca rundy albo końca gry,
- serializuje `RoundState` do `GameStateSnapshot.Data`.

Dlaczego istnieje:
- oddziela domenowy stan `RoundState` od transportowego snapshotu JSON,
- tu jest punkt translacji domena -> API.

Uwagi projektowe:
- jest silnie związany z kształtem DTO wysyłanego klientowi,
- zawiera logikę auto-progresu gry, więc nie jest tylko mapperem.

## Generic Loop Infrastructure

### `GameLoopEngineBase<TState, TMove, TCommand>`
Plik: `Services/Engine/Loop/GameLoopEngineBase.cs`

Cel:
- generyczny szkielet pętli gry typu "start albo kolejny ruch".

Użycie:
- utrzymuje aktualny stan per `gameId`,
- wymusza na klasie pochodnej implementację:
  - rozpoznania typu komendy,
  - budowy stanu początkowego,
  - pobrania legalnych ruchów,
  - mapowania komendy na ruch,
  - walidacji,
  - wyboru ruchu AI,
  - zastosowania ruchu.

Dlaczego istnieje:
- eliminuje duplikację wspólnego flow wykonania komendy,
- pozwala utrzymać logikę specyficzną dla gry w klasach potomnych.

### `GameLoopExecutionResult<TState, TMove>`
Plik: `Services/Engine/Loop/GameLoopExecutionResult.cs`

Cel:
- opisuje wynik pojedynczego przebiegu pętli gry.

Użycie:
- niesie informację, czy komenda została obsłużona,
- czy była komendą startową,
- jaki stan powstał,
- czy zastosowano ruch i jaki.

Dlaczego istnieje:
- pozwala zwrócić więcej informacji niż sam stan bez użycia wyjątków.

### `MoveValidationResult`
Plik: `Services/Engine/Loop/MoveValidationResult.cs`

Cel:
- lekki wynik walidacji ruchu.

Użycie:
- używany przez walidator ruchów,
- udostępnia fabryki `Success()` i `Failure(error)`.

### `IAiMoveStrategy<TState, TMove>`
Plik: `Services/Engine/Loop/IAiMoveStrategy.cs`

Cel:
- kontrakt wyboru ruchu dla AI.

Użycie:
- `GameLoopEngineBase` korzysta z niego pośrednio przez klasę potomną, gdy komenda nie dostarcza ruchu człowieka.

### `IMoveRuleValidator<TState, TMove, TCommand>`
Plik: `Services/Engine/Loop/IMoveRuleValidator.cs`

Cel:
- kontrakt walidacji ruchu względem stanu, komendy i listy legalnych ruchów.

Użycie:
- zwraca `MoveValidationResult`,
- pozwala wydzielić walidację poza pętlę gry.

## Haggis-Specific Engine Layer

### `HaggisServerGameLoop`
Plik: `Services/Engine/Haggis/HaggisServerGameLoop.cs`

Cel:
- implementacja generycznej pętli gry dla Haggisa,
- most pomiędzy `GameCommand` a domenowym `RoundState`/`HaggisAction`.

Użycie:
- interpretuje `Initialize`, `Play`, `Pass`, `NextMove`,
- tworzy `HaggisGame` i pierwszą rundę,
- wyciąga graczy, ustawienia scoringu i seed z `Payload`,
- mapuje `Payload` na `HaggisAction`,
- przechowuje obiekty `HaggisGame` do liczenia wyników między rundami,
- tworzy kolejne rundy i sprawdza `GameOver`.

Dlaczego istnieje:
- tu skupia się logika infrastrukturalna specyficzna dla Haggisa, ale jeszcze nie czysto domenowa,
- jest to właściwe miejsce obecnego parsowania komend.

Uwagi projektowe:
- to najważniejsza klasa do refaktoru, jeśli chcesz odejść od `JsonElement Payload`,
- ma dziś kilka odpowiedzialności:
  - command parsing,
  - inicjalizacja gry,
  - przejście między rundami,
  - konfiguracja AI,
  - konfiguracja scoringu.

### `HaggisAiMoveStrategy`
Plik: `Services/Engine/Haggis/HaggisAiMoveStrategy.cs`

Cel:
- adapter `IAiMoveStrategy` dla Haggisa.

Użycie:
- jeśli bieżący gracz to `AIPlayer`, pobiera ruch z domenowego AI,
- w przeciwnym razie zwraca pierwszy legalny ruch jako fallback.

Dlaczego istnieje:
- oddziela pętlę wykonania od konkretnej implementacji wyboru ruchu AI.

### `HaggisMoveRuleValidator`
Plik: `Services/Engine/Haggis/HaggisMoveRuleValidator.cs`

Cel:
- sprawdza, czy komenda może legalnie wykonać dany ruch w Haggisie.

Użycie:
- waliduje istnienie gracza,
- pilnuje kolejki tur,
- dla tury nie-AI wymaga `PlayerId`,
- sprawdza, czy wybrany ruch jest w zbiorze legalnych ruchów.

Dlaczego istnieje:
- trzyma walidację blisko warstwy loop/engine, bez wciskania jej do `GameLoopEngineBase`.

## Rooms i lobby

### `IGameRoomStore`
Plik: `Services/GameRooms/IGameRoomStore.cs`

Cel:
- kontrakt store'a pokojów gry.

Użycie:
- używany przez `GlobalChatHub`, `GameWebSocketHub` i `GameCommandApplicationService`.

### `GameRoomStore`
Plik: `Services/GameRooms/GameRoomStore.cs`

Cel:
- in-memory storage pokojów gry.

Użycie:
- tworzy pokój z hostem,
- listuje pokoje,
- pozwala dołączać graczom,
- zwraca klony obiektów zamiast żywych referencji.

Dlaczego istnieje:
- buduje prosty mechanizm lobby bez bazy danych,
- daje współdzielony stan dla huba globalnego i huba gry.

### `GameRoom`
Plik: `Services/GameRooms/GameRoom.cs`

Cel:
- model danych pokoju gry.

Użycie:
- przechowuje `RoomId`, `GameId`, typ gry, nazwę pokoju, datę utworzenia i listę graczy.

Dlaczego istnieje:
- to prosty obiekt stanu, bez zachowań domenowych.

## Socket Registry i obecność graczy

### `IPlayerSocketRegistry`
Plik: `Services/IPlayerSocketRegistry.cs`

Cel:
- kontrakt rejestru powiązań gracz <-> aktywne połączenia socket.

Użycie:
- używany przez oba huby.

### `PlayerSocketRegistry`
Plik: `Services/PlayerSocketRegistry.cs`

Cel:
- śledzi, które sockety należą do którego gracza,
- pozwala uzyskać sockety gracza i go wyrzucić.

Użycie:
- `Register` zapisuje nowe połączenie,
- `BindPlayer` przypisuje je do gracza,
- `Unregister` czyści rejestr,
- `KickPlayerAsync` zamyka wszystkie sockety wskazanego gracza.

Dlaczego istnieje:
- centralizuje obecność graczy ponad różnymi kanałami (`global.chat`, `games:{id}`),
- przygotowuje grunt pod administrację, presence i wysyłki per gracz.

Uwagi projektowe:
- parametr `reason` w `KickPlayerAsync` nie jest dziś używany do wysłania komunikatu klientowi.

## Chat History

### `IGlobalChatHistoryStore`
Plik: `Services/IGlobalChatHistoryStore.cs`

Cel:
- kontrakt magazynu historii globalnego czatu.

### `InMemoryGlobalChatHistoryStore`
Plik: `Services/InMemoryGlobalChatHistoryStore.cs`

Cel:
- przechowuje historię globalnego czatu w pamięci procesu.

Użycie:
- `Append` dodaje wiadomość i obcina kolejkę do 500 wpisów,
- `GetRecent` zwraca ostatnie wiadomości do bootstrapa klienta.

Dlaczego istnieje:
- oddziela strategię przechowywania historii od `GlobalChatHub`.

## Modele wiadomości gry

### `GameCommand`
Plik: `Services/Models/GameCommand.cs`

Cel:
- generyczna komenda transportowa gry.

Użycie:
- niesie `Type`, `PlayerId` i surowy `Payload`,
- jest tworzona w hubie i interpretowana później przez engine Haggisa.

Dlaczego istnieje:
- upraszcza kontrakt wejściowy WebSocket,
- pozwala jednej strukturze przenosić różne komendy.

Uwagi projektowe:
- to nie jest mocno typowana komenda domenowa,
- największa wada obecnego modelu to późna walidacja i ręczne parsowanie `Payload`.

### `GameClientMessage`
Plik: `Services/Models/GameClientMessage.cs`

Cel:
- pełna wiadomość przychodząca od klienta związana z komendą gry.

Użycie:
- zawiera `Type`, `Command` i opcjonalny `State`,
- przekazywana do `GameCommandApplicationService` i dalej do sesji.

Dlaczego istnieje:
- rozdziela envelope klienta od samej komendy.

### `GameEventMessage`
Plik: `Services/Models/GameEventMessage.cs`

Cel:
- ujednolicony komunikat wyjściowy z gry.

Użycie:
- reprezentuje `CommandApplied`, `CommandRejected`, `ChatPosted`, `ServerAnnouncement`,
- może nieść komendę, stan, błąd, czat i wskaźnik bieżącego gracza.

Dlaczego istnieje:
- upraszcza protokół wyjściowy WebSocket do jednego typu wiadomości.

### `GameStateSnapshot`
Plik: `Services/Models/GameStateSnapshot.cs`

Cel:
- transportowy snapshot stanu gry.

Użycie:
- trzyma `Version`, `Data` jako `JsonElement` i `UpdatedAt`,
- jest stanem przechowywanym w `GameSession`.

Dlaczego istnieje:
- odcina runtime klienta od domenowych typów Haggisa,
- pozwala różnych grom używać jednego kontraktu "snapshot JSON".

### `GameApplyResult`
Plik: `Services/Models/GameApplyResult.cs`

Cel:
- wynik zastosowania wiadomości do sesji.

Użycie:
- zwraca `OrderPointer` i nowy `State`.

### `GameChatMessage`
Plik: `Services/Models/GameChatMessage.cs`

Cel:
- prosty model wiadomości czatu w obrębie pokoju gry.

Użycie:
- używany jako pole w `GameChatClientMessage` i `GameEventMessage`.

### `GameChatClientMessage`
Plik: `Services/Models/GameChatClientMessage.cs`

Cel:
- wiadomość wejściowa klienta dla czatu lokalnego gry.

Użycie:
- powstaje w `GameWebSocketHub` po parsowaniu operacji `chat`.

## DTO globalnego czatu i pokojów

### `ChatMessage`
Plik: `Dtos/Chat/ChatMessage.cs`

Cel:
- zserializowana wiadomość globalnego czatu.

Użycie:
- przechowywana w historii,
- broadcastowana do klientów globalnego czatu.

### `ChatChannelSnapshot`
Plik: `Dtos/Chat/ChatChannelSnapshot.cs`

Cel:
- opis kanału widocznego w bootstrapie globalnego czatu.

Użycie:
- reprezentuje kanał globalny lub kanał powiązany z pokojem.

### `GlobalChatBootstrapMessage`
Plik: `Dtos/Chat/GlobalChatBootstrapMessage.cs`

Cel:
- pierwszy pakiet wysyłany klientowi po połączeniu z globalnym czatem.

Użycie:
- zawiera listę kanałów, historię i timestamp.

### `ProblemDetailsMessage`
Plik: `Dtos/Chat/ProblemDetailsMessage.cs`

Cel:
- prosty odpowiednik `problem details` dla błędów globalnego czatu.

Użycie:
- wysyłany przy niepoprawnym payloadzie.

### `SendChatMessageRequest`
Plik: `Dtos/Chat/SendChatMessageRequest.cs`

Cel:
- request DTO dla operacji wysłania wiadomości do globalnego czatu.

Użycie:
- parsowany w `GlobalChatHub`.

### `GameRoomResponse`
Plik: `Dtos/GameRooms/GameRoomResponse.cs`

Cel:
- DTO odpowiedzi opisujące pokój gry dla klientów lobby i gry.

Użycie:
- zwracany przy listowaniu pokojów, tworzeniu pokoju i dołączaniu do gry.

## Najważniejsze zależności architektoniczne

- `Program` składa cały graf zależności.
- `ChatWebSocketHandler` używa `GlobalChatHub`.
- `GameWebSocketHub` używa `IGameCommandApplicationService`, `IGameRoomStore`, `IPlayerSocketRegistry`.
- `GameCommandApplicationService` używa `IGameSessionStore` i `IGameRoomStore`.
- `GameSessionStore` tworzy `GameSession`.
- `GameSession` używa `IGameEngine`.
- `HaggisGameEngine` używa `HaggisServerGameLoop`.
- `HaggisServerGameLoop` używa `IAiMoveStrategy` i `IMoveRuleValidator`.

## Co warto mieć z tyłu głowy przed refaktorem

- `GameCommand`, `GameStateSnapshot` i część metod w hubach są transportowo-generyczne, ale dziś przepychają surowy JSON bardzo głęboko.
- `HaggisServerGameLoop` jest obecnie miejscem o największej liczbie odpowiedzialności i naturalnym kandydatem do rozcięcia.
- `HaggisGameEngine` robi jednocześnie orkiestrację auto-ruchów AI i mapping domeny do snapshotu API.
- `GlobalChatHub` i `GameWebSocketHub` mają sporą ilość manualnego parsowania requestów, więc przy refaktorze warto je odciążyć mapperami lub typed command handlers.
