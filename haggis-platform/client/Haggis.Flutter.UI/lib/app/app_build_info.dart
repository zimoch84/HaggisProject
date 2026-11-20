import 'package:flutter/services.dart';

class AppBuildInfo {
  AppBuildInfo._();

  static String _buildLabel = 'ui-unknown';

  static Future<void> load() async {
    final pubspec = await rootBundle.loadString('pubspec.yaml');
    final match = RegExp(
      r'^version: +(\d{4})\.(\d{1,2})\.(\d{1,2})\+(\d+)\s*$',
      multiLine: true,
    ).firstMatch(pubspec);
    if (match == null) {
      throw const FormatException('Expected pubspec version: YYYY.M.D+build');
    }
    final date =
        '${match[1]}${match[2]!.padLeft(2, '0')}'
        '${match[3]!.padLeft(2, '0')}';
    _buildLabel = 'ui-$date-${match[4]!.padLeft(3, '0')}';
  }

  static String get displayLabel => _buildLabel;
}
