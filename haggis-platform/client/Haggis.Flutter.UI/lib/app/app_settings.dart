import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:flutter/services.dart';

class AppSettings {
  AppSettings({
    required this.serverBaseUrl,
    this.androidEmulatorServerBaseUrl,
  });

  final String serverBaseUrl;
  final String? androidEmulatorServerBaseUrl;

  static Future<AppSettings> load() async {
    try {
      final jsonText = await rootBundle.loadString(
        'assets/applicationsettings.json',
      );
      final Map<String, dynamic> data =
          jsonDecode(jsonText) as Map<String, dynamic>;
      return AppSettings(
        serverBaseUrl:
            (data['serverBaseUrl'] as String?)?.trim().isNotEmpty == true
            ? (data['serverBaseUrl'] as String).trim()
            : 'http://localhost:6666',
        androidEmulatorServerBaseUrl:
            (data['androidEmulatorServerBaseUrl'] as String?)?.trim(),
      );
    } catch (_) {
      return AppSettings(
        serverBaseUrl: 'http://localhost:6666',
        androidEmulatorServerBaseUrl: 'http://10.0.2.2:6666',
      );
    }
  }

  String resolveServerBaseUrl() {
    if (!kIsWeb &&
        defaultTargetPlatform == TargetPlatform.android &&
        androidEmulatorServerBaseUrl != null &&
        androidEmulatorServerBaseUrl!.isNotEmpty) {
      return androidEmulatorServerBaseUrl!;
    }

    return serverBaseUrl;
  }
}

class ConnectionConfig {
  ConnectionConfig({
    required this.playerId,
    required this.serverBaseUrl,
  });

  String playerId;
  String serverBaseUrl;
}
