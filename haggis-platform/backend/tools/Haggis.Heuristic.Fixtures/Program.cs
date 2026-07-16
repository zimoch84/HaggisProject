using System.Text.Json;
using Haggis.AI.Model;
using Haggis.AI.Strategies;
using Haggis.Domain.Extentions;
using Haggis.Domain.Interfaces;
using Haggis.Domain.Model;

var handArg = ReadArgument(args, "--hand")
    ?? throw new ArgumentException("--hand is required");
var leadArg = ReadArgument(args, "--lead");
var hand = handArg.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToCards();
var ai = new AIPlayer("AI", HeuristicPlayStrategy.Create()) { Hand = hand };
RoundState state;

if (string.IsNullOrWhiteSpace(leadArg))
{
    state = new RoundState(new List<IHaggisPlayer>
    {
        ai,
        new HaggisPlayer("P2") { Hand = new List<string> { "10R" }.ToCards() },
        new HaggisPlayer("P3") { Hand = new List<string> { "9B" }.ToCards() }
    });
}
else
{
    var leader = new HaggisPlayer("Leader") { Hand = leadArg.ToTrick().Cards.ToList() };
    state = new RoundState(new List<IHaggisPlayer>
    {
        leader,
        ai,
        new HaggisPlayer("P3") { Hand = new List<string> { "9B" }.ToCards() }
    });
    state.ApplyAction(HaggisAction.FromTrick(leadArg, leader));
}

var selected = ai.GetPlayingAction(state);
var ranker = HeuristicActionRanker.CreateDefault();
var ranked = string.IsNullOrWhiteSpace(leadArg)
    ? ranker.RankOpeningActions(state, state.PossibleActions)
    : ranker.RankContinuationActions(state, state.PossibleActions);
Console.WriteLine(JsonSerializer.Serialize(new
{
    hand = hand.Select(card => card.ToString()),
    lead = leadArg,
    selected = selected?.Desc,
    legalActions = state.PossibleActions.Select(action => action.Desc),
    ranked = ranked.Select(candidate => new
    {
        description = candidate.Action.Desc,
        candidate.Weight,
        breakdown = candidate.Breakdown.ToDictionary(item => item.StrategyName, item => item.Weight)
    })
}));

static string? ReadArgument(string[] args, string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}
