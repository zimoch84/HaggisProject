using System.Text.Json;
using Haggis.Application.Engine.Loop;
using Haggis.Infrastructure.Services.Models;
using Haggis.Domain.Enums;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;

namespace Haggis.Infrastructure.Services.Engine.Haggis;

public sealed class HaggisServerGameLoop : GameLoopEngineBase<HaggisGameState, HaggisAction, GameCommand>
{
    private IAiMoveStrategy<HaggisGameState, HaggisAction> AiMoveStrategy { get; }
    private IMoveRuleValidator<HaggisGameState, HaggisAction, GameCommand> MoveRuleValidator { get; }

    public HaggisServerGameLoop(
        IAiMoveStrategy<HaggisGameState, HaggisAction> aiMoveStrategy,
        IMoveRuleValidator<HaggisGameState, HaggisAction, GameCommand> moveRuleValidator)
    {
        AiMoveStrategy = aiMoveStrategy;
        MoveRuleValidator = moveRuleValidator;
    }

    public bool TryExecute(string gameId, GameCommand command, out HaggisGameState? state, out HaggisAction appliedMove)
    {
        var result = Execute(gameId, command);
        if (!result.Handled || result.State is null)
        {
            state = default;
            appliedMove = default!;
            return false;
        }

        state = result.State;
        appliedMove = result.AppliedMove;
        return true;
    }

    protected override bool IsStartCommand(GameCommand command) =>
        command.Type.Equals("Initialize", StringComparison.OrdinalIgnoreCase) ||
        command.Type.Equals("Init", StringComparison.OrdinalIgnoreCase) ||
        command.Type.Equals("Start", StringComparison.OrdinalIgnoreCase);

    protected override bool IsNextMoveCommand(GameCommand command) =>
        command.Type.Equals("Play", StringComparison.OrdinalIgnoreCase) ||
        command.Type.Equals("Pass", StringComparison.OrdinalIgnoreCase) ||
        command.Type.Equals("NextMove", StringComparison.OrdinalIgnoreCase);

    protected override HaggisGameState CreateInitialState(GameCommand command)
    {
        var playerIds = ReadPlayers(command.Payload);
        if (playerIds.Count < 2)
        {
            throw new InvalidOperationException("Haggis requires at least 2 players in payload.players.");
        }

        var players = playerIds.Select(id => (IHaggisPlayer)new HaggisPlayer(id)).ToList();
        var scoringStrategy = ResolveScoringStrategy(command.Payload);
        var game = new HaggisGame(players, scoringStrategy);

        if (command.Payload.ValueKind == JsonValueKind.Object &&
            command.Payload.TryGetProperty("seed", out var seedElement) &&
            seedElement.ValueKind == JsonValueKind.Number &&
            seedElement.TryGetInt32(out var seed))
        {
            game.SetSeed(seed);
        }

        return game.NewRound();
    }

    protected override IReadOnlyList<HaggisAction> GetLegalMoves(HaggisGameState state) =>
        state.Actions.ToList();

    protected override bool TryResolveMoveFromCommand(HaggisGameState state, GameCommand command, out HaggisAction move)
    {
        move = default!;

        if (command.Type.Equals("Pass", StringComparison.OrdinalIgnoreCase))
        {
            var passAction = state.Actions.FirstOrDefault(a => a.IsPass);
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

    protected override bool ShouldUseAiMove(HaggisGameState state, GameCommand command) => state.CurrentPlayer.IsAI;

    protected override HaggisAction ResolveAiMove(HaggisGameState state, IReadOnlyList<HaggisAction> legalMoves) =>
        AiMoveStrategy.ChooseMove(state, legalMoves);

    protected override MoveValidationResult ValidateMove(
        HaggisGameState state,
        GameCommand command,
        HaggisAction move,
        IReadOnlyList<HaggisAction> legalMoves) =>
        MoveRuleValidator.Validate(state, command, move, legalMoves);

    protected override void ApplyMove(HaggisGameState state, HaggisAction move) => state.ApplyAction(move);

    private static List<string> ReadPlayers(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object ||
            !payload.TryGetProperty("players", out var playersElement) ||
            playersElement.ValueKind != JsonValueKind.Array)
        {
            return new List<string>();
        }

        return playersElement.EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.String)
            .Select(x => x.GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IHaggisPlayer ResolvePlayer(HaggisGameState state, string playerId)
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
        HaggisGameState state,
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
        var matchingAction = state.Actions.FirstOrDefault(x =>
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
