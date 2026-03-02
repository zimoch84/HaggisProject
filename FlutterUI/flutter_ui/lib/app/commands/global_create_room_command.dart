import '../models/command_input.dart';
import '../services/haggis_socket_client.dart';
import 'command_scope.dart';
import 'haggis_command.dart';

class GlobalCreateRoomCommand extends HaggisCommand {
  const GlobalCreateRoomCommand();

  @override
  String get label => 'global: createroom';

  @override
  CommandScope get scope => CommandScope.global;

  @override
  Future<void> execute(HaggisSocketClient client, CommandInput input) {
    return client.send(
      path: '/ws/global/chat',
      message: <String, dynamic>{
        'operation': 'createroom',
        'payload': <String, dynamic>{
          'playerId': input.playerId,
          'gameType': 'haggis',
          'roomName': input.roomName,
          'roomId': input.roomId,
        },
      },
    );
  }
}
