import '../models/command_input.dart';
import '../services/haggis_socket_client.dart';
import 'command_scope.dart';
import 'haggis_command.dart';

class GameCommandCommand extends HaggisCommand {
  const GameCommandCommand();

  @override
  String get label => 'game: command';

  @override
  CommandScope get scope => CommandScope.game;

  @override
  Future<void> execute(HaggisSocketClient client, CommandInput input) {
    return client.send(
      path: '/ws/games/${input.gameId}',
      message: <String, dynamic>{
        'operation': 'command',
        'payload': <String, dynamic>{
          'command': <String, dynamic>{
            'type': input.commandType,
            'playerId': input.playerId,
            'payload': <String, dynamic>{
              'note': input.text,
            },
          },
          'state': <String, dynamic>{
            'version': 1,
            'data': <String, dynamic>{},
            'updatedAt': DateTime.now().toUtc().toIso8601String(),
          },
        },
      },
    );
  }
}
