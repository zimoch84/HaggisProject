import '../models/command_input.dart';
import '../services/haggis_socket_client.dart';
import 'command_scope.dart';
import 'haggis_command.dart';

class GameChatCommand extends HaggisCommand {
  const GameChatCommand();

  @override
  String get label => 'game: chat';

  @override
  CommandScope get scope => CommandScope.game;

  @override
  Future<void> execute(HaggisSocketClient client, CommandInput input) {
    return client.send(
      path: '/ws/games/${input.gameId}',
      message: <String, dynamic>{
        'operation': 'chat',
        'payload': <String, dynamic>{
          'playerId': input.playerId,
          'text': input.text,
        },
      },
    );
  }
}
