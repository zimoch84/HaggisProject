import 'package:flutter_test/flutter_test.dart';

import 'package:flutter_ui/main.dart';

void main() {
  testWidgets('Renders lobby screen', (WidgetTester tester) async {
    await tester.pumpWidget(const HaggisApp());

    expect(find.text('Haggis Lobby'), findsOneWidget);
    expect(find.text('Lista gier'), findsOneWidget);
    expect(find.text('Connect'), findsOneWidget);
    expect(find.text('New'), findsOneWidget);
  });
}
