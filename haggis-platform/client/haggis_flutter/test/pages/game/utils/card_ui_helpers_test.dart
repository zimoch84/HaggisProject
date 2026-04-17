import 'package:flutter_test/flutter_test.dart';
import 'package:haggis_flutter/pages/game/utils/card_ui_helpers.dart';

void main() {
  group('extractCardLabels', () {
    test('does not parse Q from trick type prefix', () {
      expect(extractCardLabels('SEQ3[3R|4R|5R]'), <String>['3R', '4R', '5R']);
    });

    test('keeps wild cards from trick payload', () {
      expect(extractCardLabels('SEQ3[8O|K[9]|10O]'), <String>[
        '8O',
        'K[9]',
        '10O',
      ]);
    });

    test('still parses plain card lists without trick type prefix', () {
      expect(extractCardLabels('3R|4R|5R'), <String>['3R', '4R', '5R']);
    });
  });
}
