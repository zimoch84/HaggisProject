import '../models/command_input.dart';
import '../services/haggis_socket_client.dart';
import 'command_scope.dart';
import 'haggis_command.dart';

class GlobalPrivateChatCommand extends HaggisCommand {
  const GlobalPrivateChatCommand();

  @override
  String get label => 'global: privatechat';

  @override
  CommandScope get scope => CommandScope.global;

  @override
  Future<void> execute(HaggisSocketClient client, CommandInput input) {
    return client.send(
      path: '/ws/global/chat',
      message: <String, dynamic>{
        'operation': 'privatechat',
        'payload': <String, dynamic>{
          'playerId': input.playerId,
          'targetPlayerId': input.targetPlayerId,
          'roomName': input.roomName,
          'roomId': input.roomId,
        },
      },
    );
  }
}
