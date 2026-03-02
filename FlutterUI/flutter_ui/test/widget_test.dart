import 'package:flutter_test/flutter_test.dart';

import 'package:flutter_ui/app/haggis_command_app.dart';

void main() {
  testWidgets('Renders command console', (WidgetTester tester) async {
    await tester.pumpWidget(const HaggisCommandApp());

    expect(find.text('Haggis AsyncAPI Command Sender'), findsOneWidget);
    expect(find.text('Host'), findsOneWidget);
  });
}
