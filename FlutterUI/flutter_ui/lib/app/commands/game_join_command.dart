import '../models/command_input.dart';
import '../services/haggis_socket_client.dart';
import 'command_scope.dart';
import 'haggis_command.dart';

class GameJoinCommand extends HaggisCommand {
  const GameJoinCommand();

  @override
  String get label => 'game: join';

  @override
  CommandScope get scope => CommandScope.game;

  @override
  Future<void> execute(HaggisSocketClient client, CommandInput input) {
    return client.send(
      path: '/ws/games/${input.gameId}',
      message: <String, dynamic>{
        'operation': 'join',
        'payload': <String, dynamic>{
          'playerId': input.playerId,
        },
      },
    );
  }
}
