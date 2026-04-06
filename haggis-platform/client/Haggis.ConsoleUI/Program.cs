using Haggis.ConsoleUI.Application.AppFlow;

InitConsole.Apply();

using var cancellationSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationSource.Cancel();
};

const string serverBaseUrl = "http://localhost:5555";
string? defaultPlayerId = null;

for (var i = 0; i < args.Length; i++)
{
    var arg = args[i];
    if (arg.StartsWith("--user=", StringComparison.OrdinalIgnoreCase))
    {
        defaultPlayerId = arg["--user=".Length..];
        continue;
    }
}

await using var lobbyClient = new GlobalLobbyWebSocketClient();
await lobbyClient.ConnectAsync(serverBaseUrl, cancellationSource.Token);
await new AppFlowController(lobbyClient, defaultPlayerId).RunAsync(cancellationSource.Token);
