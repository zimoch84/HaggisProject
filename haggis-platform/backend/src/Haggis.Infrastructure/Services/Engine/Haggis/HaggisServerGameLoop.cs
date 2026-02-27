using System.Collections.Concurrent;
using System.Text.Json;
using Haggis.Infrastructure.Services.Engine.Loop;
using Haggis.AI.Interfaces;
using Haggis.AI.Model;
using Haggis.AI.StartingTrickFilterStrategies;
using Haggis.AI.Strategies;
using Haggis.Domain.Enums;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;
using Haggis.Infrastructure.Services.Models;

namespace Haggis.Infrastructure.Services.Engine.Haggis;

public sealed class HaggisServerGameLoop : GameLoopEngineBase<RoundState, HaggisAction, GameCommand>
{
    private static readonly JsonElement EmptyPayload = JsonDocument.Parse("{}").RootElement.Clone();
    private readonly ConcurrentDictionary<string, HaggisGame> _games = new();

    private IAiMoveStrategy<RoundState, HaggisAction> AiMoveStrategy { get; }
    private IMoveRuleValidator<RoundState, HaggisAction, GameCommand> MoveRuleValidator { get; }

    public HaggisServerGameLoop(
        IAiMoveStrategy<RoundState, HaggisAction> aiMoveStrategy,
        IMoveRuleValidator<RoundState, HaggisAction, GameCommand> moveRuleValidator)
    {
        AiMoveStrategy = aiMoveStrategy;
        MoveRuleValidator = moveRuleValidator;
    }

    public bool TryExecute(string gameId, GameCommand command, out RoundState? state, out HaggisAction? appliedMove)
    {
        var result = Execute(gameId, command);
        if (!result.Handled || result.State is null)
        {
            state = default;
            appliedMove = null;
            return false;
        }

        state = result.State;
        appliedMove = result.AppliedMove;
        return true;
    }

    public bool TryExecuteAiStep(string gameId, out RoundState? state, out HaggisAction? appliedMove)
    {
        return TryExecute(
            gameId,
            new GameCommand(
                Type: "NextMove",
                PlayerId: string.Empty,
                Payload: EmptyPayload),
            out state,
            out appliedMove);
    }

    public bool TryCreateNextRound(string gameId, RoundState state, out RoundState? nextRoundState)
    {
        nextRoundState = null;
        if (!state.RoundOver())
        {
            return false;
        }

        if (!_games.TryGetValue(gameId, out var game))
        {
            return false;
        }

        game.RegisterRoundScoringResult(state);
        if (game.GameOver())
        {
            return false;
        }

        nextRoundState = game.NewRound();
        SetState(gameId, nextRoundState);
        return true;
    }

    public void TryRegisterRoundScoringResult(string gameId, RoundState state)
    {
        if (_games.TryGetValue(gameId, out var game))
        {
            game.RegisterRoundScoringResult(state);
        }
    }

    public bool IsGameOver(string gameId)
    {
        return _games.TryGetValue(gameId, out var game) && game.GameOver();
    }

    public IReadOnlyDictionary<string, int> GetDisplayedScores(string gameId, RoundState state)
    {
        if (!_games.TryGetValue(gameId, out var game))
        {
            return state.Players.ToDictionary(player => player.Name, player => player.Score, StringComparer.OrdinalIgnoreCase);
        }

        var totals = new Dictionary<string, int>(game.ScoringTable.GetPlayersTotalPoints(), StringComparer.OrdinalIgnoreCase);
        var roundAlreadyRegistered = game.ScoringTable.RoundScores.Any(score => score.RoundNumber == state.RoundNumber);

        foreach (var player in state.Players)
        {
            if (!totals.ContainsKey(player.Name))
            {
                totals[player.Name] = 0;
            }

            if (!roundAlreadyRegistered)
            {
                totals[player.Name] += player.Score;
            }
        }

        return totals;
    }

    protected override bool IsStartCommand(GameCommand command) =>
        command.Type.Equals("Initialize", StringComparison.OrdinalIgnoreCase) ||
        command.Type.Equals("Init", StringComparison.OrdinalIgnoreCase) ||
        command.Type.Equals("Start", StringComparison.OrdinalIgnoreCase);

    protected override bool IsNextMoveCommand(GameCommand command) =>
        command.Type.Equals("Play", StringComparison.OrdinalIgnoreCase) ||
        command.Type.Equals("Pass", StringComparison.OrdinalIgnoreCase) ||
        command.Type.Equals("NextMove", StringComparison.OrdinalIgnoreCase);

    protected override RoundState CreateInitialState(string gameId, GameCommand command)
    {
        var players = ReadPlayers(command.Payload);
        if (players.Count < 2)
        {
            throw new InvalidOperationException("Haggis requires at least 2 players in payload.players.");
        }

        var scoringStrategy = ResolveScoringStrategy(command.Payload);
        var game = new HaggisGame(players, scoringStrategy);

        if (command.Payload.ValueKind == JsonValueKind.Object &&
            command.Payload.TryGetProperty("seed", out var seedElement) &&
            seedElement.ValueKind == JsonValueKind.Number &&
            seedElement.TryGetInt32(out var seed))
        {
            game.SetSeed(seed);
        }

        _games[gameId] = game;
        return game.NewRound();
    }

    protected override IReadOnlyList<HaggisAction> GetLegalMoves(RoundState state) =>
        state.PossibleActions.ToList();

    protected override bool TryResolveMoveFromCommand(RoundState state, GameCommand command, out HaggisAction move)
    {
        move = default!;

        if (command.Type.Equals("Pass", StringComparison.OrdinalIgnoreCase))
        {
            var passAction = state.PossibleActions.FirstOrDefault(a => a.IsPass);
            if (passAction is null)
            {
                throw new InvalidOperationException("Pass is not a legal action right now.");
            }

            move = passAction;
            return true;
        }

        if (!command.Type.Equals("Play", StringComparison.OrdinalIgnoreCase) &&
            !command.Type.Equals("NextMove", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (TryResolvePlayByActionDescription(state, command.Payload, out move))
        {
            return true;
        }

        if (PayloadHasTrick(command.Payload))
        {
            if (string.IsNullOrWhiteSpace(command.PlayerId))
            {
                return false;
            }

            var player = ResolvePlayer(state, command.PlayerId);
            if (TryResolvePlayByTrick(player, command.Payload, out move))
            {
                return true;
            }
        }

        return false;
    }

    protected override bool ShouldUseAiMove(RoundState state, GameCommand command) => state.CurrentPlayer is AIPlayer;

    protected override HaggisAction ResolveAiMove(RoundState state, IReadOnlyList<HaggisAction> legalMoves) =>
        AiMoveStrategy.ChooseMove(state, legalMoves);

    protected override MoveValidationResult ValidateMove(
        RoundState state,
        GameCommand command,
        HaggisAction move,
        IReadOnlyList<HaggisAction> legalMoves) =>
        MoveRuleValidator.Validate(state, command, move, legalMoves);

    protected override void ApplyMove(RoundState state, HaggisAction move) => state.ApplyAction(move);

    private static List<IHaggisPlayer> ReadPlayers(JsonElement payload)
    {
        var players = new List<IHaggisPlayer>();
        if (payload.ValueKind != JsonValueKind.Object ||
            !payload.TryGetProperty("players", out var playersElement) ||
            playersElement.ValueKind != JsonValueKind.Array)
        {
            return players;
        }

        foreach (var playerElement in playersElement.EnumerateArray())
        {
            var player = CreatePlayer(playerElement);
            if (player is null || players.Any(p => p.Name.Equals(player.Name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            players.Add(player);
        }

        return players;
    }

    private static IHaggisPlayer? CreatePlayer(JsonElement playerElement)
    {
        if (playerElement.ValueKind == JsonValueKind.String)
        {
            var rawPlayerId = playerElement.GetString()?.Trim();
            return string.IsNullOrWhiteSpace(rawPlayerId) ? null : new HaggisPlayer(rawPlayerId);
        }

        if (playerElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var playerId = TryReadString(playerElement, "id")
            ?? TryReadString(playerElement, "playerId")
            ?? TryReadString(playerElement, "name");
        if (string.IsNullOrWhiteSpace(playerId))
        {
            return null;
        }

        var type = TryReadString(playerElement, "type") ?? TryReadString(playerElement, "kind");
        if (!string.Equals(type, "ai", StringComparison.OrdinalIgnoreCase))
        {
            return new HaggisPlayer(playerId);
        }

        return new AIPlayer(playerId, ResolveAiPlayStrategy(playerElement));
    }

    private static IPlayStrategy ResolveAiPlayStrategy(JsonElement playerElement)
    {
        if (!TryGetObject(playerElement, "ai", out var aiElement))
        {
            return new MonteCarloStrategy(300, 25L);
        }

        var strategyName = TryReadString(aiElement, "strategy");
        if (string.Equals(strategyName, "heuristic", StringComparison.OrdinalIgnoreCase))
        {
            var useWildsInContinuations = TryReadBoolean(aiElement, "useWildsInContinuations") ??
                                          TryReadBoolean(aiElement, "heuristicUseWildsInContinuations") ??
                                          false;
            var takeLessValueTrickFirst = TryReadBoolean(aiElement, "takeLessValueTrickFirst") ?? true;

            var filter = ResolveStartingTrickFilterStrategy(aiElement, useWildsInContinuations);
            return new HeuristicPlayStrategy(
                new StartingTrickStrategy(filter),
                new ContinuationTrickStrategy(useWildsInContinuations, takeLessValueTrickFirst));
        }

        var simulations = TryReadInt(aiElement, "simulations") ?? 300;
        var timeBudgetMs = TryReadLong(aiElement, "timeBudgetMs") ?? 25L;
        return new MonteCarloStrategy(simulations, timeBudgetMs);
    }

    private static IStartingTrickFilterStrategy ResolveStartingTrickFilterStrategy(JsonElement aiElement, bool useWildsInContinuations)
    {
        var filterName = TryReadString(aiElement, "filter");
        var filterLimit = Math.Max(1, TryReadInt(aiElement, "filterLimit") ?? 5);

        if (string.Equals(filterName, "continuations", StringComparison.OrdinalIgnoreCase))
        {
            return new FilterContinuations(filterLimit, useWildsInContinuations);
        }

        if (string.Equals(filterName, "least", StringComparison.OrdinalIgnoreCase))
        {
            return new FilterXLeastValuebleStrategy(filterLimit);
        }

        if (string.Equals(filterName, "most", StringComparison.OrdinalIgnoreCase))
        {
            return new FilterXMostValuebleStrategy(filterLimit);
        }

        return new FilterNoneStrategy();
    }

    private static string? TryReadString(JsonElement source, string propertyName)
    {
        if (source.ValueKind != JsonValueKind.Object ||
            !source.TryGetProperty(propertyName, out var propertyElement) ||
            propertyElement.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var value = propertyElement.GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static int? TryReadInt(JsonElement source, string propertyName)
    {
        if (source.ValueKind != JsonValueKind.Object ||
            !source.TryGetProperty(propertyName, out var propertyElement) ||
            propertyElement.ValueKind != JsonValueKind.Number ||
            !propertyElement.TryGetInt32(out var value))
        {
            return null;
        }

        return value;
    }

    private static long? TryReadLong(JsonElement source, string propertyName)
    {
        if (source.ValueKind != JsonValueKind.Object ||
            !source.TryGetProperty(propertyName, out var propertyElement) ||
            propertyElement.ValueKind != JsonValueKind.Number ||
            !propertyElement.TryGetInt64(out var value))
        {
            return null;
        }

        return value;
    }

    private static bool? TryReadBoolean(JsonElement source, string propertyName)
    {
        if (source.ValueKind != JsonValueKind.Object ||
            !source.TryGetProperty(propertyName, out var propertyElement) ||
            propertyElement.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            return null;
        }

        return propertyElement.GetBoolean();
    }

    private static bool TryGetObject(JsonElement source, string propertyName, out JsonElement objectElement)
    {
        objectElement = default;
        return source.ValueKind == JsonValueKind.Object &&
               source.TryGetProperty(propertyName, out objectElement) &&
               objectElement.ValueKind == JsonValueKind.Object;
    }

    private static IHaggisPlayer ResolvePlayer(RoundState state, string playerId)
    {
        var player = state.Players.FirstOrDefault(p =>
            p.Name.Equals(playerId, StringComparison.OrdinalIgnoreCase));
        if (player is null)
        {
            throw new InvalidOperationException($"Player '{playerId}' is not part of this Haggis game.");
        }

        return player;
    }

    private static bool TryResolvePlayByActionDescription(
        RoundState state,
        JsonElement payload,
        out HaggisAction move)
    {
        move = default!;
        if (payload.ValueKind != JsonValueKind.Object ||
            !payload.TryGetProperty("action", out var actionElement) ||
            actionElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var actionValue = actionElement.GetString();
        var matchingAction = state.PossibleActions.FirstOrDefault(x =>
            !x.IsPass && x.Desc.Equals(actionValue, StringComparison.Ordinal));
        if (matchingAction is null)
        {
            return false;
        }

        move = matchingAction;
        return true;
    }

    private static bool TryResolvePlayByTrick(IHaggisPlayer player, JsonElement payload, out HaggisAction move)
    {
        move = default!;
        if (payload.ValueKind != JsonValueKind.Object ||
            !payload.TryGetProperty("trick", out var trickElement) ||
            trickElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        move = HaggisAction.FromTrick(trickElement.GetString()!, player);
        return true;
    }

    private static bool PayloadHasTrick(JsonElement payload) =>
        payload.ValueKind == JsonValueKind.Object &&
        payload.TryGetProperty("trick", out var trickElement) &&
        trickElement.ValueKind == JsonValueKind.String;

    private static IHaggisScoringStrategy ResolveScoringStrategy(JsonElement payload)
    {
        var defaultStrategy = new ClassicHaggisScoringStrategy();
        if (payload.ValueKind != JsonValueKind.Object ||
            !payload.TryGetProperty("options", out var optionsElement) ||
            optionsElement.ValueKind != JsonValueKind.Object ||
            !optionsElement.TryGetProperty("scoring", out var scoringElement) ||
            scoringElement.ValueKind != JsonValueKind.Object)
        {
            return defaultStrategy;
        }

        var runOutMultiplier = defaultStrategy.RunOutMultiplier;
        var gameOverScore = defaultStrategy.GameOverScore;
        if (optionsElement.TryGetProperty("winScore", out var optionsWinScoreElement) &&
            optionsWinScoreElement.ValueKind == JsonValueKind.Number &&
            optionsWinScoreElement.TryGetInt32(out var parsedOptionsWinScore) &&
            parsedOptionsWinScore > 0)
        {
            gameOverScore = parsedOptionsWinScore;
        }
        if (scoringElement.TryGetProperty("runOutMultiplier", out var multiplierElement) &&
            multiplierElement.ValueKind == JsonValueKind.Number &&
            multiplierElement.TryGetInt32(out var parsedMultiplier))
        {
            runOutMultiplier = parsedMultiplier;
        }
        if (scoringElement.TryGetProperty("gameOverScore", out var gameOverScoreElement) &&
            gameOverScoreElement.ValueKind == JsonValueKind.Number &&
            gameOverScoreElement.TryGetInt32(out var parsedGameOverScore) &&
            parsedGameOverScore > 0)
        {
            gameOverScore = parsedGameOverScore;
        }

        if (scoringElement.TryGetProperty("strategy", out var strategyElement) &&
            strategyElement.ValueKind == JsonValueKind.String)
        {
            var strategy = strategyElement.GetString();
            if (string.Equals(strategy, "EveryCardOnePoint", StringComparison.OrdinalIgnoreCase))
            {
                return new EveryCardOnePointScoringStrategy(runOutMultiplier, gameOverScore);
            }
        }

        if (!scoringElement.TryGetProperty("cardPointsByRank", out var pointsElement) ||
            pointsElement.ValueKind != JsonValueKind.Object)
        {
            return new ClassicHaggisScoringStrategy(runOutMultiplier, gameOverScore);
        }

        var pointsByRank = new Dictionary<Rank, int>();
        foreach (var property in pointsElement.EnumerateObject())
        {
            if (!TryParseRank(property.Name, out var rank) ||
                property.Value.ValueKind != JsonValueKind.Number ||
                !property.Value.TryGetInt32(out var points))
            {
                continue;
            }

            pointsByRank[rank] = points;
        }

        return new ConfigurableHaggisScoringStrategy(pointsByRank, runOutMultiplier, gameOverScore);
    }

    private static bool TryParseRank(string value, out Rank rank)
    {
        switch (value)
        {
            case "2":
                rank = Rank.TWO;
                return true;
            case "3":
                rank = Rank.THREE;
                return true;
            case "4":
                rank = Rank.FOUR;
                return true;
            case "5":
                rank = Rank.FIVE;
                return true;
            case "6":
                rank = Rank.SIX;
                return true;
            case "7":
                rank = Rank.SEVEN;
                return true;
            case "8":
                rank = Rank.EIGHT;
                return true;
            case "9":
                rank = Rank.NINE;
                return true;
            case "10":
                rank = Rank.TEN;
                return true;
            case "J":
                rank = Rank.JACK;
                return true;
            case "Q":
                rank = Rank.QUEEN;
                return true;
            case "K":
                rank = Rank.KING;
                return true;
            default:
                rank = default;
                return false;
        }
    }
}
