import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:haggis_flutter/pages/game/game_page_layout.dart';
import 'package:haggis_flutter/pages/game/models/card_ui_models.dart';
import 'package:haggis_flutter/pages/game/widgets/game_controls.dart';
import 'package:haggis_flutter/pages/game/widgets/cards.dart';
import 'package:haggis_flutter/pages/game/widgets/hand_section.dart';

void main() {
  for (final size in [
    const Size(844, 390),
    const Size(1280, 720),
    const Size(320, 700),
  ]) {
    testWidgets('board regions do not overlap at $size', (tester) async {
      tester.view.physicalSize = size;
      tester.view.devicePixelRatio = 1;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);
      final cards = [
        for (var rank = 2; rank <= 10; rank++) '${rank}R',
        'J',
        'Q',
        'K',
      ];
      String? tappedGroupCard;
      const groupedCards = ['2G', '3G', '4G'];
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: GameBoardLayout(
              handCardScale: 1.8,
              handVerticalOffset: 96,
              topRibbon: const SizedBox(key: ValueKey('ribbon'), height: 70),
              leftControls: GameActionButton(
                key: const ValueKey('left'),
                label: 'Pass',
                icon: Icons.front_hand,
                onPressed: () {},
              ),
              table: const SizedBox.expand(key: ValueKey('table')),
              rightControls: GameActionButton(
                key: const ValueKey('right'),
                label: 'Play',
                icon: Icons.play_arrow,
                onPressed: () {},
              ),
              hand: HandSection(
                cards: cards,
                savedGroup: const [],
                playableCards: cards.toSet(),
                selectedCards: const [],
                cardLabelBuilder: (card) => card,
                onCardTap: (_) async {},
                showCardHitZones: false,
                cardScale: 1.8,
                spacingScale: 1.6,
                verticalOffset: 96,
              ),
              groupedHand: PlayerHandFan(
                key: const ValueKey('group'),
                cards: groupedCards,
                playableCards: groupedCards.toSet(),
                selectedCards: const [],
                cardLabelBuilder: (card) => card,
                onCardTap: (card) async {
                  tappedGroupCard = card;
                },
                showHitZoneOutline: false,
                cardScale: 1.8,
                spacingScale: 1.6,
                verticalOffset: 36,
              ),
              handSort: HandSortToggle(
                handSortMode: HandSortMode.rank,
                onSortChanged: (_) {},
              ),
            ),
          ),
        ),
      );
      final ribbon = tester.getRect(find.byKey(const ValueKey('ribbon')));
      final left = tester.getRect(find.byKey(const ValueKey('left')));
      final table = tester.getRect(find.byKey(const ValueKey('table')));
      final right = tester.getRect(find.byKey(const ValueKey('right')));
      final hand = tester.getRect(find.byType(HandSection));
      final sort = tester.getRect(find.byType(HandSortToggle));
      expect(table.top, greaterThanOrEqualTo(ribbon.bottom));
      expect(left.right, lessThanOrEqualTo(table.left));
      expect(table.right, lessThanOrEqualTo(right.left));
      expect(hand.top, greaterThanOrEqualTo(table.bottom));
      expect(hand.bottom, lessThanOrEqualTo(size.height));
      expect(sort.overlaps(hand), isFalse);
      final group = tester.getRect(find.byKey(const ValueKey('group')));
      expect(group.right, lessThanOrEqualTo(hand.left));
      expect(hand.center.dx, closeTo(size.width / 2, 0.01));
      for (var i = 0; i < groupedCards.length; i++) {
        final rect = tester.getRect(
          find.byKey(ValueKey('hand-${groupedCards[i]}')),
        );
        final nextLeft = i + 1 < groupedCards.length
            ? tester
                  .getRect(find.byKey(ValueKey('hand-${groupedCards[i + 1]}')))
                  .left
            : rect.right;
        await tester.tapAt(Offset((rect.left + nextLeft) / 2, rect.center.dy));
        expect(tappedGroupCard, groupedCards[i]);
      }
      expect(tester.takeException(), isNull);
    });
  }
}
