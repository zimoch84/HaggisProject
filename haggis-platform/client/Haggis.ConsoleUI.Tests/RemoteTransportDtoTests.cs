using System.Text.Json;
using NUnit.Framework;

namespace Haggis.ConsoleUI.Tests;

[TestFixture]
public sealed class RemoteTransportDtoTests
{
    [Test]
    public void LobbyBootstrapDto_Deserializes_Full_Bootstrap()
    {
        const string json = """
            {
              "type": "GlobalChatBootstrap",
              "channels": [
                {
                  "channelId": "global",
                  "channelType": "global"
                },
                {
                  "channelId": "room:alpha",
                  "channelType": "room",
                  "roomId": "alpha",
                  "roomName": "Alpha",
                  "gameType": "haggis"
                }
              ],
              "history": [
                {
                  "messageId": "m1",
                  "playerId": "alice",
                  "text": "hej",
                  "createdAt": "2026-02-25T22:00:00Z"
                }
              ],
              "createdAt": "2026-02-25T22:00:00Z"
            }
            """;

        var dto = JsonSerializer.Deserialize<RemoteLobbyInboundMessageDto>(json, RemoteJsonSerializer.Options);

        Assert.Multiple(() =>
        {
            Assert.That(dto, Is.Not.Null);
            Assert.That(dto!.Type, Is.EqualTo("GlobalChatBootstrap"));
            Assert.That(dto.Channels, Has.Count.EqualTo(2));
            Assert.That(dto.Channels![1].RoomId, Is.EqualTo("alpha"));
            Assert.That(dto.History, Has.Count.EqualTo(1));
            Assert.That(dto.History![0].MessageId, Is.EqualTo("m1"));
            Assert.That(dto.CreatedAt, Is.EqualTo(DateTimeOffset.Parse("2026-02-25T22:00:00Z")));
        });
    }

    [Test]
    public void ProblemDetailsDto_Deserializes_Title_Status_And_Detail()
    {
        const string json = """
            {
              "title": "Invalid chat payload.",
              "status": 400,
              "detail": "Text is required."
            }
            """;

        var dto = JsonSerializer.Deserialize<RemoteLobbyInboundMessageDto>(json, RemoteJsonSerializer.Options);

        Assert.Multiple(() =>
        {
            Assert.That(dto, Is.Not.Null);
            Assert.That(dto!.Title, Is.EqualTo("Invalid chat payload."));
            Assert.That(dto.Status, Is.EqualTo(400));
            Assert.That(dto.Detail, Is.EqualTo("Text is required."));
        });
    }

    [Test]
    public void RoomJoinedDto_Deserializes_Full_Room_Envelope()
    {
        const string json = """
            {
              "type": "RoomJoined",
              "messageKind": "event",
              "gameId": "game-1",
              "playerId": "alice",
              "createdAt": "2026-02-25T22:00:00Z",
              "room": {
                "roomId": "game-1",
                "gameId": "game-1",
                "gameType": "haggis",
                "roomName": "Game 1",
                "createdAt": "2026-02-25T22:00:00Z",
                "players": ["alice"],
                "gameEndpoint": "/ws/games/game-1"
              }
            }
            """;

        var dto = RemoteGameMessageParser.Parse(JsonDocument.Parse(json).RootElement);

        Assert.Multiple(() =>
        {
            Assert.That(dto, Is.Not.Null);
            Assert.That(dto!.GameId, Is.EqualTo("game-1"));
            Assert.That(dto.PlayerId, Is.EqualTo("alice"));
            Assert.That(dto.Room, Is.Not.Null);
            Assert.That(dto.Room!.GameType, Is.EqualTo("haggis"));
            Assert.That(dto.Room.GameEndpoint, Is.EqualTo("/ws/games/game-1"));
        });
    }

    [Test]
    public void GameSnapshotDto_Deserializes_Extended_State_Data()
    {
        const string json = """
            {
              "type": "GameSnapshot",
              "orderPointer": 5,
              "gameId": "game-1",
              "currentPlayerId": "alice",
              "createdAt": "2026-02-25T22:00:00Z",
              "messageKind": "response",
              "state": {
                "version": 2,
                "updatedAt": "2026-02-25T22:00:01Z",
                "data": {
                  "game": "haggis",
                  "seed": 123,
                  "playerCount": 3,
                  "winScore": 500,
                  "roundNumber": 2,
                  "moveIteration": 9,
                  "currentPlayerId": "alice",
                  "roundOver": false,
                  "gameOver": false,
                  "players": [
                    {
                      "id": "alice",
                      "score": 10,
                      "handCount": 5,
                      "finished": false,
                      "isAi": false,
                      "hand": ["7S"]
                    }
                  ],
                  "trick": [],
                  "possibleActions": [],
                  "appliedMove": null,
                  "appliedMoves": [
                    {
                      "playerId": "alice",
                      "isPass": false,
                      "action": "Single: 7"
                    }
                  ],
                  "lastCommand": {
                    "type": "Play",
                    "playerId": "alice",
                    "payload": {
                      "action": "Single: 7"
                    }
                  }
                }
              }
            }
            """;

        var dto = RemoteGameMessageParser.Parse(JsonDocument.Parse(json).RootElement);

        Assert.Multiple(() =>
        {
            Assert.That(dto, Is.Not.Null);
            Assert.That(dto!.OrderPointer, Is.EqualTo(5));
            Assert.That(dto.State, Is.Not.Null);
            Assert.That(dto.State!.UpdatedAt, Is.EqualTo(DateTimeOffset.Parse("2026-02-25T22:00:01Z")));
            Assert.That(dto.State.Data!.RoundNumber, Is.EqualTo(2));
            Assert.That(dto.State.Data.AppliedMoves, Has.Count.EqualTo(1));
            Assert.That(dto.State.Data.LastCommand!.Type, Is.EqualTo("Play"));
        });
    }

    [Test]
    public void CommandAppliedDto_Deserializes_Command_Payload_With_Additional_Data()
    {
        const string json = """
            {
              "type": "CommandApplied",
              "orderPointer": 1,
              "gameId": "game-1",
              "currentPlayerId": "alice",
              "error": null,
              "command": {
                "type": "Initialize",
                "playerId": "alice",
                "payload": {
                  "seed": 123,
                  "players": ["alice", "bob", "carol"]
                }
              },
              "state": {
                "version": 1,
                "updatedAt": "2026-02-25T22:00:00Z",
                "data": {
                  "currentPlayerId": "alice",
                  "roundOver": false,
                  "players": [],
                  "trick": [],
                  "possibleActions": [],
                  "appliedMove": null
                }
              },
              "createdAt": "2026-02-25T22:00:00Z",
              "chat": null,
              "messageKind": "response"
            }
            """;

        var dto = RemoteGameMessageParser.Parse(JsonDocument.Parse(json).RootElement);

        Assert.Multiple(() =>
        {
            Assert.That(dto, Is.Not.Null);
            Assert.That(dto!.Command, Is.Not.Null);
            Assert.That(dto.Command!.Payload, Is.Not.Null);
            Assert.That(dto.Command.Payload!.Seed, Is.EqualTo(123));
            Assert.That(dto.Command.Payload.AdditionalData, Contains.Key("players"));
        });
    }

    [Test]
    public void CommandRejectedDto_Deserializes_Error_Envelope()
    {
        const string json = """
            {
              "type": "CommandRejected",
              "orderPointer": null,
              "gameId": "game-1",
              "currentPlayerId": null,
              "error": "It is not 'alice' turn.",
              "command": {
                "type": "Play",
                "playerId": "alice",
                "payload": {
                  "action": "Single: 7"
                }
              },
              "state": null,
              "createdAt": "2026-02-25T22:00:00Z",
              "chat": null,
              "messageKind": "response"
            }
            """;

        var dto = RemoteGameMessageParser.Parse(JsonDocument.Parse(json).RootElement);

        Assert.Multiple(() =>
        {
            Assert.That(dto, Is.Not.Null);
            Assert.That(dto!.Type, Is.EqualTo("CommandRejected"));
            Assert.That(dto.Error, Is.EqualTo("It is not 'alice' turn."));
            Assert.That(dto.Command!.Payload!.Action, Is.EqualTo("Single: 7"));
        });
    }

    [Test]
    public void ChatAndAnnouncementDtos_Deserializes_Chat_Envelope()
    {
        const string chatPosted = """
            {
              "type": "ChatPosted",
              "gameId": "game-1",
              "createdAt": "2026-02-25T22:00:00Z",
              "chat": {
                "playerId": "alice",
                "text": "hej pokoj"
              }
            }
            """;

        const string announcement = """
            {
              "type": "ServerAnnouncement",
              "gameId": "game-1",
              "createdAt": "2026-02-25T22:00:00Z",
              "chat": {
                "playerId": "server",
                "text": "maintenance"
              }
            }
            """;

        var chatDto = RemoteGameMessageParser.Parse(JsonDocument.Parse(chatPosted).RootElement);
        var announcementDto = RemoteGameMessageParser.Parse(JsonDocument.Parse(announcement).RootElement);

        Assert.Multiple(() =>
        {
            Assert.That(chatDto!.Chat!.Text, Is.EqualTo("hej pokoj"));
            Assert.That(announcementDto!.Chat!.PlayerId, Is.EqualTo("server"));
        });
    }

    [Test]
    public void Serialize_PrivateChat_Request_Produces_Expected_Wire_Shape()
    {
        var dto = new RemotePrivateChatRequestDto
        {
            Payload = new RemotePrivateChatPayloadDto
            {
                PlayerId = "alice",
                TargetPlayerId = "bob",
                RoomName = "Alice i Bob",
                RoomId = "room-private-1"
            }
        };

        var json = JsonSerializer.Serialize(dto, RemoteJsonSerializer.Options);

        Assert.That(json, Is.EqualTo("""{"operation":"privatechat","payload":{"playerId":"alice","targetPlayerId":"bob","roomName":"Alice i Bob","roomId":"room-private-1"}}"""));
    }

    [Test]
    public void Serialize_Create_Request_Allows_Additional_Data()
    {
        var dto = new RemoteCreateGameRequestDto
        {
            Payload = new RemoteCreateGameEnvelopeDto
            {
                PlayerId = "alice",
                Payload = new RemoteCreateGamePayloadDto
                {
                    Seed = 123,
                    AdditionalData = new Dictionary<string, JsonElement>
                    {
                        ["players"] = JsonDocument.Parse("""["alice","bob","carol"]""").RootElement.Clone()
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(dto, RemoteJsonSerializer.Options);

        Assert.That(json, Does.Contain("\"players\":[\"alice\",\"bob\",\"carol\"]"));
        Assert.That(json, Does.Contain("\"seed\":123"));
    }

    [Test]
    public void Serialize_Command_Dtos_For_Play_Pass_And_Initialize()
    {
        var play = new RemoteCommandRequestDto
        {
            Payload = new RemoteCommandEnvelopeDto
            {
                Command = new RemoteOutboundCommandDto
                {
                    Type = "Play",
                    PlayerId = "alice",
                    Payload = new RemoteGameCommandPayloadDto
                    {
                        Action = "Single: 7"
                    }
                }
            }
        };

        var pass = new RemoteCommandRequestDto
        {
            Payload = new RemoteCommandEnvelopeDto
            {
                Command = new RemoteOutboundCommandDto
                {
                    Type = "Pass",
                    PlayerId = "alice",
                    Payload = new RemoteGameCommandPayloadDto()
                }
            }
        };

        var initialize = new RemoteCommandRequestDto
        {
            Payload = new RemoteCommandEnvelopeDto
            {
                Command = new RemoteOutboundCommandDto
                {
                    Type = "Initialize",
                    PlayerId = "alice",
                    Payload = new RemoteGameCommandPayloadDto
                    {
                        Seed = 123,
                        AdditionalData = new Dictionary<string, JsonElement>
                        {
                            ["players"] = JsonDocument.Parse("""["alice","bob"]""").RootElement.Clone()
                        }
                    }
                }
            }
        };

        var playJson = JsonSerializer.Serialize(play, RemoteJsonSerializer.Options);
        var passJson = JsonSerializer.Serialize(pass, RemoteJsonSerializer.Options);
        var initJson = JsonSerializer.Serialize(initialize, RemoteJsonSerializer.Options);

        Assert.Multiple(() =>
        {
            Assert.That(playJson, Does.Contain("\"action\":\"Single: 7\""));
            Assert.That(passJson, Does.Contain("\"payload\":{}"));
            Assert.That(initJson, Does.Contain("\"players\":[\"alice\",\"bob\"]"));
            Assert.That(initJson, Does.Contain("\"seed\":123"));
        });
    }
}
