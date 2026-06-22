using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Haggis.Domain.Extentions;
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
    private static readonly object HeuristicDiagnosticsSync = new();
    private static readonly JsonElement EmptyPayload = JsonDocument.Parse("{}").RootElement.Clone();
    private const int MinSupportedPlayers = 2;
    private const int MaxSupportedPlayers = 3;
    private const int MonteCarloMediumSimulations = 300;
    private const long MonteCarloMediumTimeBudgetMs = 25L;
    private const int MonteCarloHardSimulations = 800;
    private const long MonteCarloHardTimeBudgetMs = 100L;
    private const int MonteCarloExpertSimulations = 1500;
    private const long MonteCarloExpertTimeBudgetMs = 200L;
    private readonly ConcurrentDictionary<string, HaggisGame> _games = new();
    private readonly ConcurrentDictionary<string, string> _heuristicLogPaths = new();
    private readonly ConcurrentDictionary<string, string> _heuristicCsvLogPaths = new();
    private readonly ConcurrentDictionary<string, object> _heuristicLogLocks = new();
    private readonly string _heuristicLogDirectory;

    private IAiMoveStrategy<RoundState, HaggisAction> AiMoveStrategy { get; }
    private IMoveRuleValidator<RoundState, HaggisAction, GameCommand> MoveRuleValidator { get; }

    public HaggisServerGameLoop(
        IAiMoveStrategy<RoundState, HaggisAction> aiMoveStrategy,
        IMoveRuleValidator<RoundState, HaggisAction, GameCommand> moveRuleValidator,
        IHostEnvironment hostEnvironment)
    {
        AiMoveStrategy = aiMoveStrategy;
        MoveRuleValidator = moveRuleValidator;
        _heuristicLogDirectory = ResolveHeuristicLogDirectory(hostEnvironment.ContentRootPath);
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

    public int? GetConfiguredSeed(string gameId)
    {
        if (!_games.TryGetValue(gameId, out var game))
        {
            return null;
        }

        return game.BaseSeed;
    }

    public RoundScoringResult? GetPreviousRoundResult(string gameId)
    {
        return _games.TryGetValue(gameId, out var game)
            ? game.PreviousRoundResult
            : null;
    }

    public IReadOnlyList<string> GetPreviousRoundHaggisCards(string gameId)
    {
        return _games.TryGetValue(gameId, out var game)
            ? game.PreviousRoundHaggisCards
            : Array.Empty<string>();
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
        if (players.Count < MinSupportedPlayers || players.Count > MaxSupportedPlayers)
        {
            throw new InvalidOperationException("Haggis supports only 2 or 3 players.");
        }

        var declaredPlayerCount = ReadDeclaredPlayerCount(command.Payload);
        if (declaredPlayerCount.HasValue && players.Count != declaredPlayerCount.Value)
        {
            throw new InvalidOperationException(
                $"Declared playerCount '{declaredPlayerCount.Value}' does not match payload.players count '{players.Count}'.");
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
        InitializeHeuristicLogging(gameId, game, command.Payload);
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

    protected override HaggisAction ResolveAiMove(string gameId, RoundState state, IReadOnlyList<HaggisAction> legalMoves)
    {
        if (state.CurrentPlayer is not AIPlayer aiPlayer ||
            aiPlayer.PlayStrategy is not HeuristicPlayStrategy ||
            !_heuristicLogPaths.TryGetValue(gameId, out var logPath))
        {
            return AiMoveStrategy.ChooseMove(state, legalMoves);
        }

        lock (HeuristicDiagnosticsSync)
        {
            var previousStartingDiagnosticsSink = StartingTrickStrategy.DiagnosticsSink;
            var previousContinuationDiagnosticsSink = ContinuationTrickStrategy.DiagnosticsSink;
            var playerHand = state.CurrentPlayer.Hand.ToLetters();
            var diagnosticLines = new List<string>
            {
                $"MOVE round={state.RoundNumber} move={state.MoveIteration + 1} player={state.CurrentPlayer.Name} Hand={playerHand}"
            };

            StartingTrickStrategy.DiagnosticsSink = message =>
                diagnosticLines.Add($"  heuristic: player={state.CurrentPlayer.Name} {message}");
            ContinuationTrickStrategy.DiagnosticsSink = message =>
                diagnosticLines.Add($"  heuristic: player={state.CurrentPlayer.Name} {message}");

            try
            {
                var action = AiMoveStrategy.ChooseMove(state, legalMoves);
                diagnosticLines.Add($"  selected-action: {action.Desc}");
                AppendHeuristicLogLines(gameId, logPath, diagnosticLines);
                AppendHeuristicCsvRows(
                    gameId,
                    state.RoundNumber,
                    state.MoveIteration + 1,
                    state.CurrentPlayer.Name,
                    playerHand,
                    diagnosticLines);
                return action;
            }
            finally
            {
                StartingTrickStrategy.DiagnosticsSink = previousStartingDiagnosticsSink;
                ContinuationTrickStrategy.DiagnosticsSink = previousContinuationDiagnosticsSink;
            }
        }
    }

    protected override MoveValidationResult ValidateMove(
        RoundState state,
        GameCommand command,
        HaggisAction move,
        IReadOnlyList<HaggisAction> legalMoves)
    {
        var validation = MoveRuleValidator.Validate(state, command, move, legalMoves);
        if (!validation.IsValid)
        {
            return validation;
        }

        LogHumanMoveIfConfigured(state, move);
        return validation;
    }

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

    private static int? ReadDeclaredPlayerCount(JsonElement payload)
    {
        var playerCount = TryReadInt(payload, "playerCount");
        if (playerCount is MinSupportedPlayers or MaxSupportedPlayers)
        {
            return playerCount;
        }

        return null;
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
            return new MonteCarloStrategy(MonteCarloMediumSimulations, MonteCarloMediumTimeBudgetMs);
        }

        var difficulty = TryReadInt(aiElement, "difficulty");
        if (difficulty.HasValue)
        {
            return ResolveDifficultyStrategy(difficulty.Value);
        }

        var strategyName = TryReadString(aiElement, "strategy");
        if (string.Equals(strategyName, "random", StringComparison.OrdinalIgnoreCase))
        {
            return new RandomPlayStrategy();
        }

        if (string.Equals(strategyName, "heuristic", StringComparison.OrdinalIgnoreCase))
        {
            var filter = ResolveStartingTrickFilterStrategy(aiElement);
            return HeuristicPlayStrategy.Create(startingTrickFilterStrategy: filter);
        }

        var simulations = TryReadInt(aiElement, "simulations") ?? MonteCarloMediumSimulations;
        var timeBudgetMs = TryReadLong(aiElement, "timeBudgetMs") ?? MonteCarloMediumTimeBudgetMs;
        return new MonteCarloStrategy(simulations, timeBudgetMs);
    }

    private static IPlayStrategy ResolveDifficultyStrategy(int difficulty)
    {
        return difficulty switch
        {
            1 => new RandomPlayStrategy(),
            2 => HeuristicPlayStrategy.Create(),
            3 => new MonteCarloStrategy(MonteCarloMediumSimulations, MonteCarloMediumTimeBudgetMs),
            4 => new MonteCarloStrategy(MonteCarloHardSimulations, MonteCarloHardTimeBudgetMs),
            5 => new MonteCarloStrategy(MonteCarloExpertSimulations, MonteCarloExpertTimeBudgetMs),
            _ => new MonteCarloStrategy(MonteCarloMediumSimulations, MonteCarloMediumTimeBudgetMs)
        };
    }

    private static IStartingTrickFilterStrategy ResolveStartingTrickFilterStrategy(JsonElement aiElement)
    {
        var filterName = TryReadString(aiElement, "filter");
        var filterLimit = Math.Max(1, TryReadInt(aiElement, "filterLimit") ?? 5);

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

    private void InitializeHeuristicLogging(string gameId, HaggisGame game, JsonElement payload)
    {
        var heuristicOptionSuffix = BuildHeuristicOptionSuffix(payload);
        if (string.IsNullOrWhiteSpace(heuristicOptionSuffix))
        {
            _heuristicLogPaths.TryRemove(gameId, out _);
            _heuristicCsvLogPaths.TryRemove(gameId, out _);
            _heuristicLogLocks.TryRemove(gameId, out _);
            return;
        }

        Directory.CreateDirectory(_heuristicLogDirectory);

        var fileName = $"game_seed_{game.BaseSeed}_heuristicoption{heuristicOptionSuffix}.txt";
        var filePath = Path.Combine(_heuristicLogDirectory, fileName);
        var csvFileName = $"game_seed_{game.BaseSeed}_heuristicoption{heuristicOptionSuffix}.csv";
        var csvFilePath = Path.Combine(_heuristicLogDirectory, csvFileName);
        _heuristicLogPaths[gameId] = filePath;
        _heuristicCsvLogPaths[gameId] = csvFilePath;
        _heuristicLogLocks.GetOrAdd(gameId, _ => new object());

        var headerLines = new[]
        {
            $"GAME gameId={gameId} seed={game.BaseSeed}",
            $"HEURISTIC_OPTIONS {heuristicOptionSuffix.TrimStart('_')}",
            string.Empty
        };

        File.WriteAllLines(filePath, headerLines, Encoding.UTF8);
        File.WriteAllLines(
            csvFilePath,
            new[] { "round,move_number,player,player_hand,candidate,selected_action,weight,breakdown" },
            Encoding.UTF8);
    }

    private void AppendHeuristicLogLines(string gameId, string logPath, IReadOnlyList<string> lines)
    {
        if (lines.Count == 0)
        {
            return;
        }

        var sync = _heuristicLogLocks.GetOrAdd(gameId, _ => new object());
        var content = string.Join(Environment.NewLine, lines) + Environment.NewLine;
        lock (sync)
        {
            File.AppendAllText(logPath, content, Encoding.UTF8);
        }
    }

    private void AppendHeuristicCsvRows(
        string gameId,
        int roundNumber,
        long moveNumber,
        string playerName,
        string playerHand,
        IReadOnlyList<string> diagnosticLines)
    {
        if (!_heuristicCsvLogPaths.TryGetValue(gameId, out var csvLogPath))
        {
            return;
        }

        var candidateRows = ParseHeuristicCandidateRows(
            roundNumber,
            moveNumber,
            playerName,
            playerHand,
            diagnosticLines);
        if (candidateRows.Count == 0)
        {
            return;
        }

        var sync = _heuristicLogLocks.GetOrAdd(gameId, _ => new object());
        var content = string.Join(Environment.NewLine, candidateRows.Select(BuildCsvLine)) + Environment.NewLine;
        lock (sync)
        {
            File.AppendAllText(csvLogPath, content, Encoding.UTF8);
        }
    }

    private void LogHumanMoveIfConfigured(RoundState state, HaggisAction move)
    {
        if (state.CurrentPlayer is AIPlayer)
        {
            return;
        }

        var gameId = FindGameIdForState(state);
        if (string.IsNullOrWhiteSpace(gameId) ||
            !_heuristicLogPaths.TryGetValue(gameId, out var logPath))
        {
            return;
        }

        var playerHand = state.CurrentPlayer.Hand.ToLetters();
        var diagnosticLines = new List<string>
        {
            $"MOVE round={state.RoundNumber} move={state.MoveIteration + 1} player={state.CurrentPlayer.Name} Hand={playerHand}",
            $"  selected-action: {move.Desc}"
        };

        AppendHeuristicLogLines(gameId, logPath, diagnosticLines);
        AppendHumanMoveCsvRow(
            gameId,
            state.RoundNumber,
            state.MoveIteration + 1,
            state.CurrentPlayer.Name,
            playerHand,
            move.Desc);
    }

    private string? FindGameIdForState(RoundState state)
    {
        foreach (var gameId in _heuristicLogPaths.Keys)
        {
            if (TryGetState(gameId, out var currentState) &&
                ReferenceEquals(currentState, state))
            {
                return gameId;
            }
        }

        return null;
    }

    private void AppendHumanMoveCsvRow(
        string gameId,
        int roundNumber,
        long moveNumber,
        string playerName,
        string playerHand,
        string selectedAction)
    {
        if (!_heuristicCsvLogPaths.TryGetValue(gameId, out var csvLogPath))
        {
            return;
        }

        var row = string.Join(",",
            EscapeCsv(roundNumber.ToString()),
            EscapeCsv(moveNumber.ToString()),
            EscapeCsv(playerName),
            EscapeCsv(playerHand),
            EscapeCsv(selectedAction),
            EscapeCsv("true"),
            string.Empty,
            string.Empty);

        var sync = _heuristicLogLocks.GetOrAdd(gameId, _ => new object());
        lock (sync)
        {
            File.AppendAllText(csvLogPath, row + Environment.NewLine, Encoding.UTF8);
        }
    }

    private static List<HeuristicCandidateLogRow> ParseHeuristicCandidateRows(
        int roundNumber,
        long moveNumber,
        string playerName,
        string playerHand,
        IReadOnlyList<string> diagnosticLines)
    {
        var rows = new List<HeuristicCandidateLogRow>();
        string? selectedAction = null;

        foreach (var line in diagnosticLines)
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.StartsWith("selected-action:", StringComparison.Ordinal))
            {
                selectedAction = trimmedLine.Substring("selected-action:".Length).Trim();
                continue;
            }

            var candidateMarkerIndex = trimmedLine.IndexOf("candidate:", StringComparison.Ordinal);
            if (candidateMarkerIndex < 0)
            {
                continue;
            }

            var weightMarkerIndex = trimmedLine.IndexOf(", weight:", StringComparison.Ordinal);
            var breakdownMarkerIndex = trimmedLine.IndexOf(", breakdown:", StringComparison.Ordinal);
            if (weightMarkerIndex < 0 || breakdownMarkerIndex < 0 || breakdownMarkerIndex <= weightMarkerIndex)
            {
                continue;
            }

            var candidate = trimmedLine.Substring(
                candidateMarkerIndex + "candidate:".Length,
                weightMarkerIndex - (candidateMarkerIndex + "candidate:".Length)).Trim();
            var weightText = trimmedLine.Substring(
                weightMarkerIndex + ", weight:".Length,
                breakdownMarkerIndex - (weightMarkerIndex + ", weight:".Length)).Trim();
            var breakdown = trimmedLine.Substring(breakdownMarkerIndex + ", breakdown:".Length).Trim();

            if (!int.TryParse(weightText, out var weight))
            {
                continue;
            }

            rows.Add(new HeuristicCandidateLogRow(
                roundNumber,
                moveNumber,
                playerName,
                playerHand,
                candidate,
                false,
                weight,
                breakdown));
        }

        if (!string.IsNullOrWhiteSpace(selectedAction))
        {
            var selectedRow = rows.FirstOrDefault(row =>
                row.Candidate.Equals(selectedAction, StringComparison.Ordinal));
            if (selectedRow != null)
            {
                selectedRow.SelectedAction = true;
            }
        }

        return rows;
    }

    private static string BuildCsvLine(HeuristicCandidateLogRow row)
    {
        return string.Join(",",
            EscapeCsv(row.RoundNumber.ToString()),
            EscapeCsv(row.MoveNumber.ToString()),
            EscapeCsv(row.Player),
            EscapeCsv(row.PlayerHand),
            EscapeCsv(row.Candidate),
            EscapeCsv(row.SelectedAction ? "true" : "false"),
            EscapeCsv(row.Weight.ToString()),
            EscapeCsv(row.Breakdown));
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var escaped = value.Replace("\"", "\"\"");
        return value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0
            ? $"\"{escaped}\""
            : escaped;
    }

    private static string ResolveHeuristicLogDirectory(string contentRootPath)
    {
        var backendRootPath = Path.GetFullPath(Path.Combine(contentRootPath, "..", ".."));
        return Directory.Exists(backendRootPath)
            ? backendRootPath
            : contentRootPath;
    }

    private static string BuildHeuristicOptionSuffix(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object ||
            !payload.TryGetProperty("players", out var playersElement) ||
            playersElement.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        foreach (var playerElement in playersElement.EnumerateArray())
        {
            var playerOption = BuildHeuristicPlayerOption(playerElement);
            if (!string.IsNullOrWhiteSpace(playerOption))
            {
                parts.Add(playerOption);
            }
        }

        return parts.Count == 0
            ? string.Empty
            : "_" + string.Join("__", parts.OrderBy(part => part, StringComparer.OrdinalIgnoreCase));
    }

    private static string BuildHeuristicPlayerOption(JsonElement playerElement)
    {
        if (playerElement.ValueKind != JsonValueKind.Object)
        {
            return string.Empty;
        }

        var playerId = TryReadString(playerElement, "id")
            ?? TryReadString(playerElement, "playerId")
            ?? TryReadString(playerElement, "name");
        if (string.IsNullOrWhiteSpace(playerId) ||
            !string.Equals(TryReadString(playerElement, "type") ?? TryReadString(playerElement, "kind"), "ai", StringComparison.OrdinalIgnoreCase) ||
            !TryGetObject(playerElement, "ai", out var aiElement))
        {
            return string.Empty;
        }

        var parts = new List<string>
        {
            $"player-{SanitizeForFileName(playerId)}"
        };

        var difficulty = TryReadInt(aiElement, "difficulty");
        if (difficulty == 2)
        {
            parts.Add("difficulty-2");
            return string.Join("_", parts);
        }

        var strategyName = TryReadString(aiElement, "strategy");
        if (!string.Equals(strategyName, "heuristic", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        parts.Add("strategy-heuristic");

        var filter = TryReadString(aiElement, "filter");
        if (!string.IsNullOrWhiteSpace(filter))
        {
            parts.Add($"filter-{SanitizeForFileName(filter)}");
        }

        var filterLimit = TryReadInt(aiElement, "filterLimit");
        if (filterLimit.HasValue)
        {
            parts.Add($"filterLimit-{filterLimit.Value}");
        }

        return string.Join("_", parts);
    }

    private static string SanitizeForFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unknown";
        }

        var builder = new StringBuilder(value.Length);
        foreach (var character in value.Trim())
        {
            builder.Append(char.IsLetterOrDigit(character) || character is '-' or '_'
                ? character
                : '-');
        }

        return builder.ToString().Trim('-');
    }

    private sealed class HeuristicCandidateLogRow
    {
        public HeuristicCandidateLogRow(
            int roundNumber,
            long moveNumber,
            string player,
            string playerHand,
            string candidate,
            bool selectedAction,
            int weight,
            string breakdown)
        {
            RoundNumber = roundNumber;
            MoveNumber = moveNumber;
            Player = player;
            PlayerHand = playerHand;
            Candidate = candidate;
            SelectedAction = selectedAction;
            Weight = weight;
            Breakdown = breakdown;
        }

        public int RoundNumber { get; }
        public long MoveNumber { get; }
        public string Player { get; }
        public string PlayerHand { get; }
        public string Candidate { get; }
        public bool SelectedAction { get; set; }
        public int Weight { get; }
        public string Breakdown { get; }
    }
}
