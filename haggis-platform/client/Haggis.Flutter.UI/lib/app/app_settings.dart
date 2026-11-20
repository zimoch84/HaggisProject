import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:flutter/services.dart';

class AppSettings {
  AppSettings({
    required this.serverBaseUrl,
    this.androidServerBaseUrls = const <String>[],
  });

  final String serverBaseUrl;
  final List<String> androidServerBaseUrls;

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
        androidServerBaseUrls: _parseAndroidServerBaseUrls(
          data['androidServerBaseUrls'],
        ),
      );
    } catch (_) {
      return AppSettings(
        serverBaseUrl: 'http://localhost:6666',
        androidServerBaseUrls: const <String>[
          'http://192.168.0.17:6666',
          'http://10.0.2.2:6666',
        ],
      );
    }
  }

  List<String> resolveCandidateServerBaseUrls({
    bool? isWebOverride,
    TargetPlatform? targetPlatformOverride,
  }) {
    final isWeb = isWebOverride ?? kIsWeb;
    final platform = targetPlatformOverride ?? defaultTargetPlatform;
    if (!isWeb && platform == TargetPlatform.android) {
      final normalizedAndroidUrls = androidServerBaseUrls
          .map((String value) => value.trim())
          .where((String value) => value.isNotEmpty)
          .toList(growable: false);
      if (normalizedAndroidUrls.isNotEmpty) {
        return normalizedAndroidUrls;
      }
    }

    return <String>[serverBaseUrl.trim()];
  }

  String resolveServerBaseUrl({
    bool? isWebOverride,
    TargetPlatform? targetPlatformOverride,
  }) {
    return resolveCandidateServerBaseUrls(
      isWebOverride: isWebOverride,
      targetPlatformOverride: targetPlatformOverride,
    ).first;
  }

  static List<String> _parseAndroidServerBaseUrls(Object? rawValue) {
    final urls = (rawValue as List<dynamic>? ?? const <dynamic>[])
        .map((dynamic value) => value.toString().trim())
        .where((String value) => value.isNotEmpty)
        .toList(growable: false);
    if (urls.isNotEmpty) {
      return urls;
    }

    return const <String>[
      'http://192.168.0.17:6666',
      'http://10.0.2.2:6666',
    ];
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
