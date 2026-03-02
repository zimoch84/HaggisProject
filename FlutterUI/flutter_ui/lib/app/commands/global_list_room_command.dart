import '../models/command_input.dart';
import '../services/haggis_socket_client.dart';
import 'command_scope.dart';
import 'haggis_command.dart';

class GlobalListRoomCommand extends HaggisCommand {
  const GlobalListRoomCommand();

  @override
  String get label => 'global: listroom';

  @override
  CommandScope get scope => CommandScope.global;

  @override
  Future<void> execute(HaggisSocketClient client, CommandInput input) {
    return client.send(
      path: '/ws/global/chat',
      message: <String, dynamic>{
        'operation': 'listroom',
        'payload': <String, dynamic>{},
      },
    );
  }
}
