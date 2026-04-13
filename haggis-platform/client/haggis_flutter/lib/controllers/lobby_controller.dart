import 'dart:async';
import 'package:flutter/material.dart';

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

  LobbyViewModel get viewModel => LobbyViewModel(
        playerId: playerId,
        status: status,
        rooms: List<LobbyRoom>.unmodifiable(rooms),
        messages: List<LobbyChatMessage>.unmodifiable(messages),
        isBusy: _isBusy,
      );

  Future<void> connect() async {
    await _client.connect();
    _subscription = _client.messages.listen(
      _onMessage,
      onError: (Object error, StackTrace _) {
        status = 'Lobby socket error: $error';
        notifyListeners();
      },
      onDone: () {
        status = 'Lobby socket closed.';
        notifyListeners();
      },
    );
    await Future<void>.delayed(const Duration(milliseconds: 100));
    refreshRooms();
  }

  void refreshRooms() {
    _isBusy = true;
    _client.requestRoomList();
    status = 'Refreshing rooms...';
    notifyListeners();
  }

  Future<LobbyRoom?> createRoom(String playerId, String roomName) async {
    final normalizedName =
        roomName.trim().isEmpty ? "$playerId's room" : roomName.trim();
    final roomId = _slugify(normalizedName);
    _pendingCreatedRoom?.complete(null);
    _pendingCreatedRoom = Completer<LobbyRoom?>();
    _client.createRoom(
      playerId: playerId,
      roomName: normalizedName,
      roomId: roomId,
    );
    _isBusy = true;
    status = "Creating room '$normalizedName'...";
    notifyListeners();
    final createdRoom = await _pendingCreatedRoom!.future.timeout(
      const Duration(seconds: 5),
      onTimeout: () => null,
    );
    if (createdRoom == null) {
      refreshRooms();
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

    if (type == 'GlobalChatBootstrap') {
      messages
        ..clear()
        ..addAll(
          ((json['history'] as List<dynamic>? ?? <dynamic>[]))
              .map(
                (dynamic message) => LobbyChatMessage.fromJson(
                  message as Map<String, dynamic>,
                ),
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
      _isBusy = false;
      status = 'Loaded ${rooms.length} rooms.';
      notifyListeners();
      return;
    }

    if (operation == 'createroom' || operation == 'privatechat') {
      final data = json['data'] as Map<String, dynamic>? ?? <String, dynamic>{};
      final roomJson = data['room'] as Map<String, dynamic>?;
      if (roomJson != null) {
        final room = LobbyRoom.fromJson(roomJson);
        final existingIndex =
            rooms.indexWhere((LobbyRoom item) => item.roomId == room.roomId);
        if (existingIndex >= 0) {
          rooms[existingIndex] = room;
        } else {
          rooms.add(room);
        }
        _isBusy = false;
        status = "Created room '${room.roomName}'.";
        _pendingCreatedRoom?.complete(room);
        _pendingCreatedRoom = null;
      }
      notifyListeners();
      return;
    }

    if (json['detail'] != null || json['error'] != null) {
      _isBusy = false;
      _pendingCreatedRoom?.complete(null);
      _pendingCreatedRoom = null;
      status = (json['detail'] ?? json['error']).toString();
      notifyListeners();
    }
  }

  String _slugify(String value) {
    return value
        .trim()
        .toLowerCase()
        .replaceAll(RegExp(r'[^a-z0-9]+'), '-')
        .replaceAll(RegExp(r'-+'), '-')
        .replaceAll(RegExp(r'^-|-$'), '');
  }
}
