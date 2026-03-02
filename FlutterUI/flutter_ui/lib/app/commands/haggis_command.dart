import '../models/command_input.dart';
import '../services/haggis_socket_client.dart';
import 'command_scope.dart';

abstract class HaggisCommand {
  const HaggisCommand();

  String get label;
  CommandScope get scope;

  Future<void> execute(
    HaggisSocketClient client,
    CommandInput input,
  );
}
