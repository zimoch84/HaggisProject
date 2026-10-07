import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import 'app/app_build_info.dart';
import 'app/app_settings.dart';
import 'app/haggis_app.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await SystemChrome.setPreferredOrientations([
    DeviceOrientation.landscapeLeft,
    DeviceOrientation.landscapeRight,
  ]);
  await AppBuildInfo.load();
  final appSettings = await AppSettings.load();
  runApp(HaggisFlutterApp(appSettings: appSettings));
}
