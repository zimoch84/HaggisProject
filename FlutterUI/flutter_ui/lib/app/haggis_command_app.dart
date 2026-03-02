import 'package:flutter/material.dart';

import 'pages/command_console_page.dart';

class HaggisCommandApp extends StatelessWidget {
   const HaggisCommandApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      title: 'Haggis Command Console',
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xFF1F6E8C)),
        useMaterial3: true,
      ),
      home: const CommandConsolePage(),
    );
  }
}
