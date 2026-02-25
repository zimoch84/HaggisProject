# WebSocket messages do Postmana

Ten dokument opisuje aktualne komunikaty WebSocket dla `Haggis.Infrastructure` i gotowe payloady do testow w Postmanie.

## Endpointy

- healthcheck HTTP: `GET http://localhost:5555/`
- global chat WS: `ws://localhost:5555/ws/global/chat`
- game room WS: `ws://localhost:5555/ws/games/{gameId}`

Jesli endpoint WS zostanie wywolany zwyklym HTTP GET, serwer zwroci:

- status: `400 BadRequest`
- body: `WebSocket connection expected.`

## Konwencja payloadu klienta

Kazda wiadomosc wysylana przez klienta ma format:

```json
{
  "operation": "nazwa-operacji",
  "payload": {}
}
```

`operation` jest parsowane case-insensitive.

## 1) Global chat (`/ws/global/chat`)

Po podlaczeniu serwer od razu wysyla bootstrap:

```json
{
  "type": "GlobalChatBootstrap",
  "channels": [
    {
      "channelId": "global",
      "channelType": "global"
    }
  ],
  "history": [],
  "createdAt": "2026-02-25T22:00:00Z"
}
```

### Operacja `chat`

Request:

```json
{
  "operation": "chat",
  "payload": {
    "playerId": "alice",
    "text": "hej wszystkim"
  }
}
```

Broadcast response (do wszystkich klientow podlaczonych do global chat):

```json
{
  "messageId": "8f971d6f63fe45ff84dca2c40209cd34",
  "playerId": "alice",
  "text": "hej wszystkim",
  "createdAt": "2026-02-25T22:00:00Z"
}
```

### Operacja `listroom`

Request:

```json
{
  "operation": "listroom",
  "payload": {}
}
```

Response:

```json
{
  "operation": "listroom",
  "data": {
    "rooms": [
      {
        "roomId": "room-alpha",
        "gameId": "room-alpha",
        "gameType": "haggis",
        "roomName": "room-alpha",
        "createdAt": "2026-02-25T22:00:00Z",
        "players": ["alice", "bob"],
        "gameEndpoint": "/ws/games/room-alpha"
      }
    ],
    "createdAt": "2026-02-25T22:00:00Z"
  }
}
```

### Operacja `createroom`

`roomId` jest opcjonalny. Jesli go nie podasz, backend wygeneruje GUID (`Guid.NewGuid().ToString("N")`).

Request:

```json
{
  "operation": "createroom",
  "payload": {
    "playerId": "alice",
    "gameType": "haggis",
    "roomName": "Alice room"
  }
}
```

Response:

```json
{
  "operation": "createroom",
  "data": {
    "room": {
      "roomId": "6f4f341cca014e32b5756e44f3ed97f1",
      "gameId": "6f4f341cca014e32b5756e44f3ed97f1",
      "gameType": "haggis",
      "roomName": "Alice room",
      "createdAt": "2026-02-25T22:00:00Z",
      "players": ["alice"],
      "gameEndpoint": "/ws/games/6f4f341cca014e32b5756e44f3ed97f1"
    },
    "gameEndpoint": "/ws/games/6f4f341cca014e32b5756e44f3ed97f1",
    "createdAt": "2026-02-25T22:00:00Z"
  }
}
```

### Operacja `privatechat`

Request:

```json
{
  "operation": "privatechat",
  "payload": {
    "playerId": "alice",
    "targetPlayerId": "bob",
    "roomName": "Alice i Bob",
    "roomId": "room-private-1"
  }
}
```

Response:

```json
{
  "operation": "privatechat",
  "data": {
    "room": {
      "roomId": "room-private-1",
      "gameId": "room-private-1",
      "gameType": "haggis",
      "roomName": "Alice i Bob",
      "createdAt": "2026-02-25T22:00:00Z",
      "players": ["alice", "bob"],
      "gameEndpoint": "/ws/games/room-private-1"
    },
    "gameEndpoint": "/ws/games/room-private-1",
    "createdAt": "2026-02-25T22:00:00Z"
  }
}
```

### Bledy na global chat

Nieprawidlowy payload dla `chat`:

```json
{
  "title": "Invalid chat payload.",
  "status": 400
}
```

Nieznana operacja:

```json
{
  "operation": "unknown",
  "data": {
    "type": "OperationRejected",
    "error": "Unsupported operation 'unknown'.",
    "createdAt": "2026-02-25T22:00:00Z"
  }
}
```

## 2) Game room (`/ws/games/{gameId}`)

### Operacja `join`

Request:

```json
{
  "operation": "join",
  "payload": {
    "playerId": "alice"
  }
}
```

Broadcast response:

```json
{
  "type": "RoomJoined",
  "gameId": "game-1",
  "playerId": "alice",
  "room": {
    "roomId": "game-1",
    "gameId": "game-1",
    "gameType": "haggis",
    "roomName": "game-1",
    "createdAt": "2026-02-25T22:00:00Z",
    "players": ["alice"],
    "gameEndpoint": "/ws/games/game-1"
  },
  "createdAt": "2026-02-25T22:00:00Z"
}
```

### Operacja `create`

`create` tworzy/uzupelnia pokoj i uruchamia komende `Initialize`.

Request:

```json
{
  "operation": "create",
  "payload": {
    "playerId": "alice",
    "payload": {
      "players": ["alice", "bob", "carol"],
      "seed": 123
    }
  }
}
```

Response: taki sam format jak `command` (`CommandApplied` albo `CommandRejected`).

### Operacja `command`

Request:

```json
{
  "operation": "command",
  "payload": {
    "command": {
      "type": "Initialize",
      "playerId": "alice",
      "payload": {
        "players": ["alice", "bob", "carol"],
        "seed": 123
      }
    }
  }
}
```

Mozliwy request dla ruchu:

```json
{
  "operation": "command",
  "payload": {
    "command": {
      "type": "Play",
      "playerId": "alice",
      "payload": {
        "action": "Single: 7"
      }
    }
  }
}
```

Mozliwy request dla pass:

```json
{
  "operation": "command",
  "payload": {
    "command": {
      "type": "Pass",
      "playerId": "alice",
      "payload": {}
    }
  }
}
```

Opcjonalnie mozna podac stan bazowy (nadpisuje `CurrentState` jako input symulacji):

```json
{
  "operation": "command",
  "payload": {
    "command": {
      "type": "Sync",
      "playerId": "alice",
      "payload": {
        "state": {
          "round": 2
        }
      }
    },
    "state": {
      "version": 5,
      "data": {
        "round": 1
      },
      "updatedAt": "2026-01-01T00:00:00Z"
    }
  }
}
```

`CommandApplied` response:

Pole `CurrentPlayerId` wskazuje, czyja jest tura po wykonaniu komendy. Dla `Initialize` jest to gracz rozpoczynajacy gre.

```json
{
  "Type": "CommandApplied",
  "OrderPointer": 1,
  "GameId": "game-1",
  "CurrentPlayerId": "alice",
  "Error": null,
  "Command": {
    "Type": "Initialize",
    "PlayerId": "alice",
    "Payload": {
      "players": ["alice", "bob", "carol"],
      "seed": 123
    }
  },
  "State": {
    "Version": 1,
    "Data": {
      "game": "haggis",
      "currentPlayerId": "alice",
      "roundOver": false,
      "players": [],
      "trick": [],
      "possibleActions": [],
      "appliedMove": null,
      "lastCommand": {
        "type": "Initialize",
        "playerId": "alice"
      }
    },
    "UpdatedAt": "2026-02-25T22:00:00Z"
  },
  "CreatedAt": "2026-02-25T22:00:00Z",
  "Chat": null
}
```

`CommandRejected` response:

```json
{
  "Type": "CommandRejected",
  "OrderPointer": null,
  "GameId": "game-1",
  "CurrentPlayerId": null,
  "Error": "It is not 'alice' turn.",
  "Command": {
    "Type": "Play",
    "PlayerId": "alice",
    "Payload": {
      "action": "Single: 7"
    }
  },
  "State": null,
  "CreatedAt": "2026-02-25T22:00:00Z",
  "Chat": null
}
```

### Operacja `chat`

Request:

```json
{
  "operation": "chat",
  "payload": {
    "playerId": "alice",
    "text": "hej pokoj"
  }
}
```

Broadcast response:

```json
{
  "Type": "ChatPosted",
  "OrderPointer": null,
  "GameId": "game-1",
  "Error": null,
  "Command": null,
  "State": null,
  "CreatedAt": "2026-02-25T22:00:00Z",
  "Chat": {
    "PlayerId": "alice",
    "Text": "hej pokoj"
  }
}
```

### Bledy na game room

Nieznana operacja / zly payload:

```json
{
  "type": "OperationRejected",
  "gameId": "game-1",
  "error": "Invalid command payload.",
  "createdAt": "2026-02-25T22:00:00Z"
}
```

Komenda/chat od gracza niebedacego w pokoju:

```json
{
  "Type": "CommandRejected",
  "OrderPointer": null,
  "GameId": "game-1",
  "Error": "Player is not joined to this room.",
  "Command": {
    "Type": "Play",
    "PlayerId": "eve",
    "Payload": {}
  },
  "State": null,
  "CreatedAt": "2026-02-25T22:00:00Z",
  "Chat": null
}
```

albo dla czatu:

```json
{
  "Type": "ChatRejected",
  "OrderPointer": null,
  "GameId": "game-1",
  "Error": "Player is not joined to this room.",
  "Command": null,
  "State": null,
  "CreatedAt": "2026-02-25T22:00:00Z",
  "Chat": null
}
```

## Szybki flow testowy w Postmanie

1. Otworz `ws://localhost:5555/ws/global/chat`.
2. Odbierz `GlobalChatBootstrap`.
3. Wyslij `createroom` i skopiuj `room.gameEndpoint`.
4. Otworz nowe polaczenie na `ws://localhost:5555{gameEndpoint}`.
5. Wyslij `join`.
6. Wyslij `command` z `Initialize`.
7. Wyslij `chat` na roomie.
