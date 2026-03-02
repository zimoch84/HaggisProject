import 'game_chat_command.dart';
import 'game_command_command.dart';
import 'game_create_command.dart';
import 'game_join_command.dart';
import 'global_chat_command.dart';
import 'global_create_room_command.dart';
import 'global_list_room_command.dart';
import 'global_private_chat_command.dart';
import 'haggis_command.dart';

class CommandCatalog {
  static const List<HaggisCommand> all = <HaggisCommand>[
    GlobalChatCommand(),
    GlobalListRoomCommand(),
    GlobalCreateRoomCommand(),
    GlobalPrivateChatCommand(),
    GameJoinCommand(),
    GameCreateCommand(),
    GameChatCommand(),
    GameCommandCommand(),
  ];
}
