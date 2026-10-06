import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:haggis_flutter/pages/game/models/card_ui_models.dart';
import 'package:haggis_flutter/pages/game/widgets/cards.dart';
import 'package:haggis_flutter/pages/game/widgets/hand_section.dart';

void main() {
  for (final width in [320.0, 700.0]) {
    testWidgets('full hand fits and remains tappable at width $width', (tester) async {
      final cards = [for (var rank = 2; rank <= 10; rank++) '${rank}R',
        for (var rank = 2; rank <= 6; rank++) '${rank}G', 'J', 'Q', 'K'];
      String? tapped;
      await tester.pumpWidget(MaterialApp(home: Scaffold(body: Align(
        alignment: Alignment.topLeft,
        child: SizedBox(width: width, height: 148, child: HandSection(
          cards: cards, savedGroup: const [], playableCards: cards.toSet(),
          selectedCards: [cards.first], cardLabelBuilder: (card) => card,
          onCardTap: (card) async { tapped = card; },
          handSortMode: HandSortMode.rank, onSortChanged: (_) {},
          onGroupColorBomb: null, showCardHitZones: false,
          cardScale: 1.8, spacingScale: 1.6, verticalOffset: 96,
        )),
      ))));
      final bounds = tester.getRect(find.byType(HandSection));
      for (var index = 0; index < cards.length; index++) {
        final rect = tester.getRect(find.byKey(ValueKey('hand-${cards[index]}')));
        expect(rect.left, greaterThanOrEqualTo(bounds.left));
        expect(rect.right, lessThanOrEqualTo(bounds.right));
        expect(rect.top, greaterThanOrEqualTo(bounds.top + 44));
        expect(rect.bottom, lessThanOrEqualTo(bounds.bottom));
        final nextLeft = index + 1 < cards.length
            ? tester.getRect(find.byKey(ValueKey('hand-${cards[index + 1]}'))).left
            : rect.right;
        await tester.tapAt(Offset((rect.left + nextLeft) / 2, rect.center.dy));
        expect(tapped, cards[index]);
      }
      expect(find.byType(HandCard), findsNWidgets(17));
      expect(tester.takeException(), isNull);
    });
  }
}
