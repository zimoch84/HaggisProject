# Post-Login Flow (Session, Chat, Room, Game)

## Scenariusz po zalogowaniu

1. Gracz laczy sie z serwerem i jest tworzona sesja online (socket + playerId).
2. Gracz dostaje globalny chat:
- ostatnie 50 wiadomosci,
- strumien nowych wiadomosci.
3. Gracz moze:
- stworzyc pokoj (`POST /api/gamerooms`),
- dolaczyc do pokoju (`POST /api/gamerooms/{roomId}/join`).
4. W singleplayer pokoj moze zawierac 1 gracza ludzkiego + 2 AI.
5. Gracze rozmawiaja przez pokojowy chat (`/ws/chat/rooms/{roomId}`).
6. Host klika start:
- tworzona jest gra (`gameId`),
- event startu jest broadcastowany do aktywnych graczy w pokoju,
- gracze lacza sie do `/games/{gameId}/actions`.

## Flow

```text
[Client] --WS login--> [Session Gateway]
  -> session created (playerId, socket)
  -> OnlinePlayers[playerId] = socket

[Session Gateway] -> [Global Chat]
[Global Chat] -> send last 50 messages -> [Client]
[Global Chat] -> stream new messages -> [Client]

Create room:
[Client host] --POST /api/gamerooms--> [Room Service]
  -> create room(roomId, gameType, host)
  -> RoomPresence[roomId] add host socket

Join room:
[Client player] --POST /api/gamerooms/{roomId}/join--> [Room Service]
  -> add player to room
  -> RoomPresence[roomId] add player socket

Singleplayer:
[Client host] -> room players = [host, ai-1, ai-2]
  -> RoomPresence contains only human sockets

Room chat:
[Clients] <--> /ws/chat/rooms/{roomId} <--> [RoomChatHub]
  -> validate player belongs to room
  -> broadcast to sockets in RoomPresence[roomId]

Start game (host):
[Host] --Start--> [Room Service]
  -> validate host and room state
  -> create gameId
  -> room state = InGame
  -> broadcast GameStarted(gameId, endpoint) to room sockets

Game loop:
[Players] --WS connect--> /games/{gameId}/actions --> [GameWebSocketHub]
  -> GameClients[gameId] add sockets
  -> player command received (Initialize/Play/Pass/...)
  -> engine apply command
  -> broadcast event/state to all sockets in GameClients[gameId]
  -> AI commands are produced by engine and broadcasted the same way
```

## In-memory mapy (model logiczny)

1. `OnlinePlayers[playerId] = sessionSocket`
2. `RoomPresence[roomId] = set(playerId, socket)` (ludzie online)
3. `GameClients[gameId] = set(socket)` (subskrybenci wydarzen gry)

## Uwagi implementacyjne

1. `Serwer.API` obsluguje gameroom i chat.
2. `Haggis.Infrastructure` obsluguje gameplay websocket (`/games/{gameId}/actions`).
3. Gdy serwisy sa rozdzielone host/port, klient musi laczyc sie do hosta gry dla `gameEndpoint`.
