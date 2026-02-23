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
