import 'dart:async';
import 'package:flutter/material.dart';

import '../infrastructure/logging/app_logger.dart';
import '../infrastructure/remote/global_lobby_websocket_client.dart';
import '../models/lobby_models.dart';
import '../view_models/lobby_view_model.dart';

class LobbyController extends ChangeNotifier {
  LobbyController(this.serverBaseUrl, this.playerId)
    : _client = GlobalLobbyWebSocketClient(serverBaseUrl);

  final String serverBaseUrl;
  final String playerId;
  final GlobalLobbyWebSocketClient _client;
  final List<LobbyRoom> rooms = <LobbyRoom>[];
  final List<LobbyChatMessage> messages = <LobbyChatMessage>[];
  StreamSubscription<Map<String, dynamic>>? _subscription;
  String status = 'Connecting...';
  bool _isBusy = false;
  Completer<LobbyRoom?>? _pendingCreatedRoom;
  String? _pendingCreatedRoomId;

  LobbyViewModel get viewModel => LobbyViewModel(
    playerId: playerId,
    status: status,
    rooms: List<LobbyRoom>.unmodifiable(rooms),
    messages: List<LobbyChatMessage>.unmodifiable(messages),
    isBusy: _isBusy,
  );

  Future<void> connect() async {
    AppLogger.info('LobbyCtrl', 'Starting lobby connect via $serverBaseUrl.');
    await _client.connect();
    _subscription = _client.messages.listen(
      _onMessage,
      onError: (Object error, StackTrace _) {
        status = 'Lobby socket error: $error';
        AppLogger.error('LobbyCtrl', 'Lobby subscription error.', error);
        notifyListeners();
      },
      onDone: () {
        status = 'Lobby socket closed.';
        AppLogger.warn('LobbyCtrl', 'Lobby subscription closed.');
        notifyListeners();
      },
    );
    await Future<void>.delayed(const Duration(milliseconds: 100));
    refreshRooms();
  }

  void refreshRooms() {
    _isBusy = true;
    AppLogger.info('LobbyCtrl', 'Refreshing room list.');
    _client.requestRoomList();
    status = 'Refreshing rooms...';
    notifyListeners();
  }

  Future<LobbyRoom?> createRoom(String playerId, String roomName) async {
    final normalizedName = roomName.trim().isEmpty
        ? "$playerId's room"
        : roomName.trim();
    final roomId = _buildRoomId(normalizedName);
    _pendingCreatedRoom?.complete(null);
    _pendingCreatedRoom = Completer<LobbyRoom?>();
    _pendingCreatedRoomId = roomId;
    _isBusy = true;
    status = "Creating room '$normalizedName'...";
    AppLogger.info('LobbyCtrl', 'Creating room "$normalizedName" ($roomId).');
    notifyListeners();

    try {
      _client.createRoom(
        playerId: playerId,
        roomName: normalizedName,
        roomId: roomId,
      );
    } catch (error) {
      _pendingCreatedRoom?.complete(null);
      _pendingCreatedRoom = null;
      _pendingCreatedRoomId = null;
      _isBusy = false;
      status = 'Could not send create room request: $error';
      notifyListeners();
      return null;
    }

    final createdRoom = await _pendingCreatedRoom!.future.timeout(
      const Duration(seconds: 5),
      onTimeout: () => null,
    );
    if (createdRoom == null) {
      _pendingCreatedRoom = null;
      _pendingCreatedRoomId = null;
      _isBusy = false;
      status = "Could not create room '$normalizedName'.";
      notifyListeners();
    }
    return createdRoom;
  }

  @override
  void dispose() {
    _subscription?.cancel();
    _client.dispose();
    super.dispose();
  }

  void _onMessage(Map<String, dynamic> json) {
    final type = (json['type'] ?? '').toString();
    final operation = (json['operation'] ?? '').toString();
    AppLogger.info('LobbyCtrl', 'Received message type=$type operation=$operation.');

    if (type == 'GlobalChatBootstrap') {
      messages
        ..clear()
        ..addAll(
          ((json['history'] as List<dynamic>? ?? <dynamic>[])).map(
            (dynamic message) =>
                LobbyChatMessage.fromJson(message as Map<String, dynamic>),
          ),
        );
      status = 'Connected to public lobby.';
      notifyListeners();
      return;
    }

    if (json['messageId'] != null) {
      messages.add(LobbyChatMessage.fromJson(json));
      if (messages.length > 20) {
        messages.removeAt(0);
      }
      notifyListeners();
      return;
    }

    if (operation == 'listroom') {
      final data = json['data'] as Map<String, dynamic>? ?? <String, dynamic>{};
      final listedRooms = data['rooms'] as List<dynamic>? ?? <dynamic>[];
      rooms
        ..clear()
        ..addAll(
          listedRooms.map(
            (dynamic room) => LobbyRoom.fromJson(room as Map<String, dynamic>),
          ),
        );
      final pendingRoom = _findPendingCreatedRoom();
      _isBusy = false;
      status = 'Loaded ${rooms.length} rooms.';
      if (pendingRoom != null && _pendingCreatedRoom?.isCompleted == false) {
        status = "Created room '${pendingRoom.roomName}'.";
        _pendingCreatedRoom?.complete(pendingRoom);
        _pendingCreatedRoom = null;
        _pendingCreatedRoomId = null;
      }
      notifyListeners();
      return;
    }

    if (operation == 'createroom' || operation == 'privatechat') {
      final data = json['data'] as Map<String, dynamic>? ?? <String, dynamic>{};
      final error = (data['error'] ?? '').toString();
      if (error.isNotEmpty) {
        _isBusy = false;
        status = error;
        _pendingCreatedRoom?.complete(null);
        _pendingCreatedRoom = null;
        _pendingCreatedRoomId = null;
        notifyListeners();
        return;
      }

      final roomJson = data['room'] as Map<String, dynamic>?;
      if (roomJson != null) {
        final room = LobbyRoom.fromJson(roomJson);
        final existingIndex = rooms.indexWhere(
          (LobbyRoom item) => item.roomId == room.roomId,
        );
        if (existingIndex >= 0) {
          rooms[existingIndex] = room;
        } else {
          rooms.add(room);
        }
        _isBusy = false;
        status = "Created room '${room.roomName}'.";
        _pendingCreatedRoom?.complete(room);
        _pendingCreatedRoom = null;
        _pendingCreatedRoomId = null;
      }
      notifyListeners();
      return;
    }

    if (json['detail'] != null || json['error'] != null) {
      _isBusy = false;
      _pendingCreatedRoom?.complete(null);
      _pendingCreatedRoom = null;
      _pendingCreatedRoomId = null;
      status = (json['detail'] ?? json['error']).toString();
      notifyListeners();
    }
  }

  LobbyRoom? _findPendingCreatedRoom() {
    final pendingRoomId = _pendingCreatedRoomId;
    if (pendingRoomId == null || pendingRoomId.isEmpty) {
      return null;
    }

    for (final room in rooms) {
      if (room.roomId == pendingRoomId) {
        return room;
      }
    }

    return null;
  }

  String _buildRoomId(String value) {
    final slug = value
        .trim()
        .toLowerCase()
        .replaceAll(RegExp(r'[^a-z0-9]+'), '-')
        .replaceAll(RegExp(r'-+'), '-')
        .replaceAll(RegExp(r'^-|-$'), '');
    if (slug.isNotEmpty) {
      return slug;
    }

    return DateTime.now().microsecondsSinceEpoch.toString();
  }
}
