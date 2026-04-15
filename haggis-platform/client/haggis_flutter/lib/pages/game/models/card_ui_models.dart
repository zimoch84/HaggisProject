enum HandSortMode { rank, color }

class ParsedCardLabel {
  const ParsedCardLabel({
    required this.raw,
    required this.rankOrder,
    required this.suitOrder,
  });

  final String raw;
  final int rankOrder;
  final int suitOrder;

  factory ParsedCardLabel.fromRaw(String rawLabel) {
    final raw = rawLabel.trim().toUpperCase();
    if (raw.isEmpty) {
      return const ParsedCardLabel(raw: '', rankOrder: 999, suitOrder: 999);
    }

    final rankToken = raw.length > 1 ? raw.substring(0, raw.length - 1) : raw;
    final suitToken = raw.length > 1 ? raw.substring(raw.length - 1) : '';

    return ParsedCardLabel(
      raw: raw,
      rankOrder: _rankOrder(rankToken),
      suitOrder: _suitOrder(suitToken),
    );
  }

  static int _rankOrder(String rankToken) {
    const ranks = {
      '2': 2,
      '3': 3,
      '4': 4,
      '5': 5,
      '6': 6,
      '7': 7,
      '8': 8,
      '9': 9,
      '10': 10,
      'J': 11,
      'Q': 12,
      'K': 13,
      'A': 14,
    };
    return ranks[rankToken] ?? 999;
  }

  static int _suitOrder(String suitToken) {
    const suits = {'B': 0, 'G': 1, 'R': 2, 'O': 3, 'Y': 4};
    return suits[suitToken] ?? 999;
  }
}

class DisplayCardLabel {
  const DisplayCardLabel({
    required this.rankToken,
    required this.suitToken,
    required this.accentToken,
    required this.assignmentRankToken,
    required this.assignmentSuitToken,
    required this.isWildAssignment,
  });

  final String rankToken;
  final String suitToken;
  final String accentToken;
  final String assignmentRankToken;
  final String assignmentSuitToken;
  final bool isWildAssignment;

  factory DisplayCardLabel.fromLabel(String rawLabel) {
    final raw = rawLabel.trim().toUpperCase();
    if (raw.isEmpty) {
      return const DisplayCardLabel(
        rankToken: '?',
        suitToken: '?',
        accentToken: '?',
        assignmentRankToken: '',
        assignmentSuitToken: '',
        isWildAssignment: false,
      );
    }
    if (raw.contains('[')) {
      final bracketIndex = raw.indexOf('[');
      final assignment = raw.substring(bracketIndex + 1, raw.length - 1);
      final parsedAssignment = _splitRankAndSuit(assignment);
      return DisplayCardLabel(
        rankToken: raw.substring(0, bracketIndex),
        suitToken: parsedAssignment.$2,
        accentToken: _extractAccentToken(assignment),
        assignmentRankToken: parsedAssignment.$1,
        assignmentSuitToken: parsedAssignment.$2,
        isWildAssignment: true,
      );
    }
    if (raw.length == 1) {
      return DisplayCardLabel(
        rankToken: raw,
        suitToken: '?',
        accentToken: '?',
        assignmentRankToken: '',
        assignmentSuitToken: '',
        isWildAssignment: false,
      );
    }

    final parsed = _splitRankAndSuit(raw);
    return DisplayCardLabel(
      rankToken: parsed.$1,
      suitToken: parsed.$2,
      accentToken: parsed.$2,
      assignmentRankToken: '',
      assignmentSuitToken: '',
      isWildAssignment: false,
    );
  }

  static String _extractAccentToken(String assignment) {
    final match = RegExp(r'([BGROY])$').firstMatch(assignment);
    return match?.group(1) ?? '';
  }

  static (String, String) _splitRankAndSuit(String token) {
    final trimmed = token.trim().toUpperCase();
    if (trimmed.isEmpty) {
      return ('?', '?');
    }
    if (trimmed.length == 1) {
      return (trimmed, '?');
    }
    return (
      trimmed.substring(0, trimmed.length - 1),
      trimmed.substring(trimmed.length - 1),
    );
  }
}
