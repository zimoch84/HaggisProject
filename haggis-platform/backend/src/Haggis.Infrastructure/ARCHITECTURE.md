# Haggis.Infrastructure Layering (DDD-oriented)

This service is split into three concerns:

1. `Services.Hubs` (Transport)
- WebSocket I/O and payload parsing.
- No game transition logic.

2. `Services.Application`
- Use-case orchestration (`GameCommandApplicationService`).
- Converts command execution result into domain events (`CommandApplied` / `CommandRejected`).

3. `Services.Infrastructure`
- Session persistence (`GameSessionStore`, `GameSession`) and state progression through `IGameEngine`.

Game rules remain in `Haggis.Domain` and are invoked through `HaggisGameEngine`.
AI strategies and heuristics live in `Haggis.AI`.

## Game Flow And Commands

- `Initialize` creates a persisted `HaggisGame` per `gameId`, starts round `1`, and returns engine snapshot in `CommandApplied`.
- `Play` / `Pass` apply one human move and then auto-progress AI turns until another human turn, next round, or game over.
- Round transition is automatic:
- if `roundOver=true` and no player reached cumulative `winScore` from `ScoringTable`, server opens a new round immediately.
- if `roundOver=true` and some player reached cumulative `winScore`, server returns `gameOver=true`.

## Player Configuration In `Initialize.payload.players`

Supported forms:

- simple string: `"alice"` (human player)
- object:

```json
{
  "id": "bot-1",
  "type": "ai",
  "ai": {
    "strategy": "montecarlo",
    "simulations": 20,
    "timeBudgetMs": 1
  }
}
```

Heuristic AI example:

```json
{
  "id": "bot-2",
  "type": "ai",
  "ai": {
    "strategy": "heuristic",
    "useWildsInContinuations": true,
    "takeLessValueTrickFirst": true,
    "filter": "continuations",
    "filterLimit": 5
  }
}
```

`State.Data.players[*]` now includes `isAi` and `State.Data.appliedMoves` includes all moves executed during one command (player move plus auto AI chain).
