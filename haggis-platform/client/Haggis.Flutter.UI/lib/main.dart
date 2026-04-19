import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import 'app/app_settings.dart';
import 'app/haggis_app.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await SystemChrome.setPreferredOrientations([
    DeviceOrientation.landscapeLeft,
    DeviceOrientation.landscapeRight,
  ]);
  final appSettings = await AppSettings.load();
  runApp(HaggisFlutterApp(appSettings: appSettings));
}
