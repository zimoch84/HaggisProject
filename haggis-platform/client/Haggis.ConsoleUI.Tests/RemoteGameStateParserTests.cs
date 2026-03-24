using System.Text.Json;
using NUnit.Framework;

namespace Haggis.ConsoleUI.Tests;

[TestFixture]
public sealed class RemoteGameStateParserTests
{
    [Test]
    public void ParseSnapshot_Maps_All_Currently_Used_Fields()
    {
        using var document = JsonDocument.Parse("""
            {
              "version": 7,
              "data": {
                "currentPlayerId": "p2",
                "roundOver": true,
                "players": [
                  {
                    "id": "p1",
                    "score": 11,
                    "handCount": 3,
                    "finished": false,
                    "hand": ["3S", "4H"]
                  }
                ],
                "trick": [
                  {
                    "playerId": "p2",
                    "isPass": false,
                    "desc": "Pair 7"
                  }
                ],
                "possibleActions": [
                  {
                    "type": "Play",
                    "action": "7 7"
                  }
                ],
                "appliedMove": {
                  "playerId": "p2",
                  "isPass": false,
                  "action": "Pair 7"
                }
              }
            }
            """);

        var state = RemoteGameStateParser.ParseSnapshot(document.RootElement);

        Assert.Multiple(() =>
        {
            Assert.That(state.Version, Is.EqualTo(7));
            Assert.That(state.CurrentPlayerId, Is.EqualTo("p2"));
            Assert.That(state.RoundOver, Is.True);
            Assert.That(state.Players, Has.Count.EqualTo(1));
            Assert.That(state.Players[0].Id, Is.EqualTo("p1"));
            Assert.That(state.Players[0].Score, Is.EqualTo(11));
            Assert.That(state.Players[0].HandCount, Is.EqualTo(3));
            Assert.That(state.Players[0].Finished, Is.False);
            Assert.That(state.Players[0].Hand, Is.EqualTo(new[] { "3S", "4H" }));
            Assert.That(state.Trick, Has.Count.EqualTo(1));
            Assert.That(state.Trick[0].Description, Is.EqualTo("Pair 7"));
            Assert.That(state.PossibleActions, Has.Count.EqualTo(1));
            Assert.That(state.PossibleActions[0].Type, Is.EqualTo("Play"));
            Assert.That(state.PossibleActions[0].Action, Is.EqualTo("7 7"));
            Assert.That(state.AppliedMove, Is.Not.Null);
            Assert.That(state.AppliedMove!.PlayerId, Is.EqualTo("p2"));
            Assert.That(state.AppliedMove.Action, Is.EqualTo("Pair 7"));
        });
    }

    [Test]
    public void ParseSnapshot_Uses_Defaults_For_Missing_Optional_Fields()
    {
        using var document = JsonDocument.Parse("""
            {
              "version": 5,
              "data": {
                "players": [
                  {
                    "id": "p1"
                  }
                ]
              }
            }
            """);

        var state = RemoteGameStateParser.ParseSnapshot(document.RootElement);

        Assert.Multiple(() =>
        {
            Assert.That(state.Version, Is.EqualTo(5));
            Assert.That(state.CurrentPlayerId, Is.EqualTo(string.Empty));
            Assert.That(state.RoundOver, Is.False);
            Assert.That(state.Players, Has.Count.EqualTo(1));
            Assert.That(state.Players[0].Score, Is.EqualTo(0));
            Assert.That(state.Players[0].HandCount, Is.EqualTo(0));
            Assert.That(state.Players[0].Finished, Is.False);
            Assert.That(state.Players[0].Hand, Is.Empty);
            Assert.That(state.Trick, Is.Empty);
            Assert.That(state.PossibleActions, Is.Empty);
            Assert.That(state.AppliedMove, Is.Null);
        });
    }

    [Test]
    public void ParseSnapshot_Leaves_AppliedMove_Null_When_Payload_Has_Null()
    {
        using var document = JsonDocument.Parse("""
            {
              "version": 2,
              "data": {
                "appliedMove": null
              }
            }
            """);

        var state = RemoteGameStateParser.ParseSnapshot(document.RootElement);

        Assert.That(state.AppliedMove, Is.Null);
    }

    [Test]
    public void ParseSnapshot_Handles_Empty_Arrays()
    {
        using var document = JsonDocument.Parse("""
            {
              "version": 3,
              "data": {
                "players": [],
                "trick": [],
                "possibleActions": []
              }
            }
            """);

        var state = RemoteGameStateParser.ParseSnapshot(document.RootElement);

        Assert.Multiple(() =>
        {
            Assert.That(state.Players, Is.Empty);
            Assert.That(state.Trick, Is.Empty);
            Assert.That(state.PossibleActions, Is.Empty);
        });
    }

    [Test]
    public void ParseSnapshot_Returns_Empty_State_For_Malformed_Snapshot_Shape()
    {
        using var document = JsonDocument.Parse("""
            [
              {
                "version": 1
              }
            ]
            """);

        var state = RemoteGameStateParser.ParseSnapshot(document.RootElement);

        AssertEmptyState(state);
    }

    [Test]
    public void ParseSnapshot_Is_Case_Insensitive()
    {
        using var document = JsonDocument.Parse("""
            {
              "VERSION": 9,
              "DATA": {
                "CURRENTPLAYERID": "p3",
                "ROUNDOVER": true,
                "PLAYERS": [
                  {
                    "ID": "p3",
                    "SCORE": 42,
                    "HANDCOUNT": 1,
                    "FINISHED": true,
                    "HAND": ["J"]
                  }
                ],
                "TRICK": [
                  {
                    "PLAYERID": "p3",
                    "ISPASS": true,
                    "DESC": "Pass"
                  }
                ],
                "POSSIBLEACTIONS": [
                  {
                    "TYPE": "Pass",
                    "ACTION": ""
                  }
                ],
                "APPLIEDMOVE": {
                  "PLAYERID": "p3",
                  "ISPASS": true,
                  "ACTION": "Pass"
                }
              }
            }
            """);

        var state = RemoteGameStateParser.ParseSnapshot(document.RootElement);

        Assert.Multiple(() =>
        {
            Assert.That(state.Version, Is.EqualTo(9));
            Assert.That(state.CurrentPlayerId, Is.EqualTo("p3"));
            Assert.That(state.RoundOver, Is.True);
            Assert.That(state.Players[0].Id, Is.EqualTo("p3"));
            Assert.That(state.Trick[0].Description, Is.EqualTo("Pass"));
            Assert.That(state.PossibleActions[0].Type, Is.EqualTo("Pass"));
            Assert.That(state.AppliedMove, Is.Not.Null);
            Assert.That(state.AppliedMove!.Action, Is.EqualTo("Pass"));
        });
    }

    private static void AssertEmptyState(RemoteGameState state)
    {
        Assert.Multiple(() =>
        {
            Assert.That(state.Version, Is.EqualTo(0));
            Assert.That(state.CurrentPlayerId, Is.EqualTo(string.Empty));
            Assert.That(state.RoundOver, Is.False);
            Assert.That(state.Players, Is.Empty);
            Assert.That(state.Trick, Is.Empty);
            Assert.That(state.PossibleActions, Is.Empty);
            Assert.That(state.AppliedMove, Is.Null);
        });
    }
}
