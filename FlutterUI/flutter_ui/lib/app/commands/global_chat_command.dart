import '../models/command_input.dart';
import '../services/haggis_socket_client.dart';
import 'command_scope.dart';
import 'haggis_command.dart';

class GlobalChatCommand extends HaggisCommand {
  const GlobalChatCommand();

  @override
  String get label => 'global: chat';

  @override
  CommandScope get scope => CommandScope.global;

  @override
  Future<void> execute(HaggisSocketClient client, CommandInput input) {
    return client.send(
      path: '/ws/global/chat',
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
