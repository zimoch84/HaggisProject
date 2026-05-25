import 'dart:math';

import 'package:flutter/material.dart';
import '../models/card_ui_models.dart';
import '../utils/card_ui_helpers.dart';

class TableCard extends StatelessWidget {
  const TableCard({super.key, required this.label});

  final String label;

  @override
  Widget build(BuildContext context) {
    final display = DisplayCardLabel.fromLabel(label);
    final centerSuitAssetPath = display.isWildAssignment
        ? cardSuitAssetPath(display.assignmentSuitToken)
        : cardSuitAssetPath(display.suitToken);
    final centerWildAssetPath = wildCardAssetPath(display.rankToken);

    return Container(
      width: 96,
      height: 136,
      decoration: BoxDecoration(
        color: const Color(0xFFF7F1E6),
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: const Color(0xFFD8CFBD), width: 1.5),
        boxShadow: const [
          BoxShadow(
            color: Color(0x22000000),
            blurRadius: 6,
            offset: Offset(0, 3),
          ),
        ],
      ),
      child: Stack(
        children: [
          Positioned(
            top: 10,
            left: 10,
            child: CardCorner(display: display, compact: false),
          ),
          Center(
            child: CenterCardMark(
              suitAssetPath: centerSuitAssetPath,
              wildAssetPath: centerWildAssetPath,
              color:
                  (display.accentToken.isEmpty
                          ? const Color(0xFF8A5B1F)
                          : cardAccent(display.accentToken))
                      .withValues(alpha: 0.24),
            ),
          ),
          Positioned(
            right: 10,
            bottom: 10,
            child: Transform.rotate(
              angle: 3.14159,
              child: CardCorner(display: display, compact: false),
            ),
          ),
        ],
      ),
    );
  }
}

class HandCard extends StatelessWidget {
  const HandCard({
    super.key,
    required this.label,
    required this.isPlayable,
    required this.isSelected,
    this.showHitZoneOutline = false,
    this.scale = 1,
    this.onTap,
  });

  final String label;
  final bool isPlayable;
  final bool isSelected;
  final bool showHitZoneOutline;
  final double scale;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final display = DisplayCardLabel.fromLabel(label);
    final centerSuitAssetPath = display.isWildAssignment
        ? cardSuitAssetPath(display.assignmentSuitToken)
        : cardSuitAssetPath(display.suitToken);
    final wildAssetPath = wildMiniAssetPath(display.rankToken);

    return GestureDetector(
      onTap: onTap,
      child: AnimatedScale(
        scale: isSelected ? 1.03 : 1,
        duration: const Duration(milliseconds: 160),
        curve: Curves.easeOutCubic,
        child: Opacity(
          opacity: isPlayable || isSelected ? 1 : 0.82,
          child: Container(
            width: 64 * scale,
            height: 98 * scale,
            decoration: BoxDecoration(
              color: const Color(0xFFF6F1E7),
              borderRadius: BorderRadius.circular(16 * scale.clamp(0.9, 1.2)),
              border: Border.all(
                color: isSelected
                    ? const Color(0xFFF2C14E)
                    : const Color(0xFFD8CFBD),
                width: isSelected || isPlayable ? 2 : 1,
              ),
              boxShadow: [
                BoxShadow(
                  color: isSelected
                      ? const Color(0x44F2C14E)
                      : const Color(0x22000000),
                  blurRadius: isSelected ? 14 : 6,
                  offset: Offset(0, isSelected ? 8 : 3),
                ),
              ],
            ),
            child: Stack(
              children: [
                Padding(
                  padding: EdgeInsets.all(8 * scale.clamp(0.9, 1.2)),
                  child: Stack(
                    children: [
                      Center(
                        child: CenterCardMark(
                          suitAssetPath: centerSuitAssetPath,
                          wildAssetPath: wildAssetPath,
                          color:
                              (display.accentToken.isEmpty
                                      ? const Color(0xFF8A5B1F)
                                      : cardAccent(display.accentToken))
                                  .withValues(alpha: 0.2),
                          compact: true,
                        ),
                      ),
                      Align(
                        alignment: Alignment.topLeft,
                        child: Container(
                          padding: EdgeInsets.symmetric(
                            horizontal: 8 * scale.clamp(0.9, 1.2),
                            vertical: 6 * scale.clamp(0.9, 1.2),
                          ),
                          decoration: BoxDecoration(
                            color: const Color(0xFFF0E8D8),
                            borderRadius: BorderRadius.circular(
                              10 * scale.clamp(0.9, 1.2),
                            ),
                            border: Border.all(color: const Color(0x22B4A893)),
                          ),
                          child: CardCorner(display: display, compact: true),
                        ),
                      ),
                    ],
                  ),
                ),
                if (showHitZoneOutline)
                  Positioned.fill(
                    child: IgnorePointer(
                      child: DecoratedBox(
                        decoration: BoxDecoration(
                          borderRadius: BorderRadius.circular(
                            16 * scale.clamp(0.9, 1.2),
                          ),
                          border: Border.all(
                            color: isSelected
                                ? const Color(0xCCFFB000)
                                : const Color(0xCCFF4D4D),
                            width: 2,
                          ),
                        ),
                      ),
                    ),
                  ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class CardCorner extends StatelessWidget {
  const CardCorner({super.key, required this.display, required this.compact});

  final DisplayCardLabel display;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    final rankSize = compact ? 16.0 : 22.0;
    final suitSize = compact ? 13.0 : 18.0;
    final color = display.accentToken.isEmpty
        ? const Color(0xFF8A5B1F)
        : cardAccent(display.accentToken);

    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        Text(
          display.rankToken,
          style: TextStyle(
            color: color,
            fontSize: rankSize,
            fontWeight: FontWeight.w900,
            height: 1,
          ),
        ),
        if (display.isWildAssignment) ...[
          const SizedBox(height: 2),
          Text(
            display.assignmentRankToken,
            style: TextStyle(
              color: color,
              fontSize: suitSize,
              fontWeight: FontWeight.w800,
              height: 1,
            ),
          ),
          if (display.assignmentSuitToken.isNotEmpty)
            SuitSymbolMark(
              suitToken: display.assignmentSuitToken,
              size: suitSize,
              color: color,
            ),
        ] else if (display.suitToken.isNotEmpty) ...[
          const SizedBox(height: 2),
          SuitSymbolMark(
            suitToken: display.suitToken,
            size: suitSize,
            color: color,
          ),
        ],
      ],
    );
  }
}

class CenterCardMark extends StatelessWidget {
  const CenterCardMark({
    super.key,
    required this.suitAssetPath,
    required this.wildAssetPath,
    required this.color,
    this.compact = false,
  });

  final String? suitAssetPath;
  final String? wildAssetPath;
  final Color color;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    if (wildAssetPath != null) {
      return Opacity(
        opacity: compact ? 0.88 : 0.95,
        child: Image.asset(
          wildAssetPath!,
          width: compact ? 32 : 50,
          height: compact ? 32 : 50,
          fit: BoxFit.contain,
        ),
      );
    }

    if (suitAssetPath == null) {
      return const SizedBox.shrink();
    }

    return ColorFiltered(
      colorFilter: ColorFilter.mode(color, BlendMode.modulate),
      child: Image.asset(
        suitAssetPath!,
        width: compact ? 30 : 42,
        height: compact ? 30 : 42,
        fit: BoxFit.contain,
      ),
    );
  }
}

class SuitSymbolMark extends StatelessWidget {
  const SuitSymbolMark({
    super.key,
    required this.suitToken,
    required this.size,
    this.color,
  });

  final String suitToken;
  final double size;
  final Color? color;

  @override
  Widget build(BuildContext context) {
    final assetPath = cardSuitAssetPath(suitToken);
    if (assetPath == null) {
      return Text(
        suitToken,
        style: TextStyle(
          color: color ?? Colors.white,
          fontSize: size,
          fontWeight: FontWeight.w800,
          height: 1,
        ),
      );
    }

    Widget child = Image.asset(assetPath, width: size, height: size);
    if (color != null) {
      child = ColorFiltered(
        colorFilter: ColorFilter.mode(color!, BlendMode.modulate),
        child: child,
      );
    }

    return child;
  }
}

class CardGeometry {
  const CardGeometry({
    required this.index,
    required this.left,
    required this.top,
    required this.width,
    required this.height,
    required this.angle,
    required this.visibleHitWidth,
  });

  final int index;
  final double left;
  final double top;
  final double width;
  final double height;
  final double angle;
  final double visibleHitWidth;

  Rect get visibleHitRect => Rect.fromLTWH(left, top, visibleHitWidth, height);

  bool containsPoint(Offset point) {
    return visibleHitRect.contains(point);
  }

  bool containsPointWithRotation(Offset point) {
    if (angle == 0) {
      return containsPoint(point);
    }

    final pivot = Offset(left + width / 2, top + height);
    final translated = point - pivot;
    final sinAngle = -sin(angle);
    final cosAngle = cos(angle);
    final unrotated = Offset(
      translated.dx * cosAngle - translated.dy * sinAngle,
      translated.dx * sinAngle + translated.dy * cosAngle,
    );

    return visibleHitRect.contains(unrotated + pivot);
  }
}

class PlayerHandFan extends StatelessWidget {
  const PlayerHandFan({
    super.key,
    required this.cards,
    required this.playableCards,
    required this.selectedCards,
    required this.cardLabelBuilder,
    required this.onCardTap,
    required this.showHitZoneOutline,
    required this.cardScale,
    required this.spacingScale,
    required this.arcScale,
    required this.verticalOffset,
  });

  final List<String> cards;
  final Set<String> playableCards;
  final List<String> selectedCards;
  final String Function(String card) cardLabelBuilder;
  final Future<void> Function(String card) onCardTap;
  final bool showHitZoneOutline;
  final double cardScale;
  final double spacingScale;
  final double arcScale;
  final double verticalOffset;

  @override
  Widget build(BuildContext context) {
    final cardWidth = 62.0 * cardScale;
    final preferredStep = 32.0 * spacingScale;
    final minimumStep = 14.0 * spacingScale;
    final cardHeight = 98.0 * cardScale;
    final naturalFanHeight = cardHeight + (48 * cardScale) + (24 * arcScale);

    return LayoutBuilder(
      builder: (BuildContext context, BoxConstraints constraints) {
        final availableWidth = constraints.maxWidth.isFinite
            ? constraints.maxWidth
            : cardWidth;
        final fanHeight = constraints.maxHeight.isFinite
            ? constraints.maxHeight
            : naturalFanHeight;
        final fittedStep = cards.length <= 1
            ? cardWidth
            : (availableWidth - cardWidth) / (cards.length - 1);
        final effectiveStep = cards.length <= 1
            ? cardWidth
            : fittedStep.clamp(minimumStep, preferredStep).toDouble();
        final contentWidth = cards.isEmpty
            ? cardWidth
            : cardWidth + (cards.length - 1) * effectiveStep;
        final viewportWidth = contentWidth < availableWidth
            ? availableWidth
            : contentWidth;
        final horizontalInset = (viewportWidth - contentWidth) / 2;
        final verticalAdjustment = _resolveVerticalAdjustment(
          cardHeight: cardHeight,
          fanHeight: fanHeight,
        );

        return SingleChildScrollView(
          scrollDirection: Axis.horizontal,
          child: GestureDetector(
            behavior: HitTestBehavior.opaque,
            onTapUp: cards.isEmpty
                ? null
                : (TapUpDetails details) {
                    final card = _cardAtOffset(
                      localOffset: details.localPosition,
                      startOffset: horizontalInset,
                      step: effectiveStep,
                      cardWidth: cardWidth,
                      cardHeight: cardHeight,
                      fanHeight: fanHeight,
                      verticalAdjustment: verticalAdjustment,
                    );
                    if (card != null) {
                      onCardTap(card);
                    }
                  },
            child: _fanHitBox(
              width: viewportWidth,
              height: fanHeight,
              child: Stack(
                clipBehavior: Clip.hardEdge,
                children: [
                  for (int index = 0; index < cards.length; index++)
                    _positionedHandCard(
                      index: index,
                      count: cards.length,
                      step: effectiveStep,
                      startOffset: horizontalInset,
                      verticalAdjustment: verticalAdjustment,
                      label: cards[index],
                      displayLabel: cardLabelBuilder(cards[index]),
                      isPlayable: playableCards.contains(cards[index]),
                      cardWidth: cardWidth,
                      cardHeight: cardHeight,
                      fanHeight: fanHeight,
                    ),
                ],
              ),
            ),
          ),
        );
      },
    );
  }

  double _resolveVerticalAdjustment({
    required double cardHeight,
    required double fanHeight,
  }) {
    if (cards.isEmpty) {
      return 0;
    }

    var minTop = double.infinity;
    var maxBottom = 0.0;
    for (var index = 0; index < cards.length; index++) {
      final top = _baseCardTop(
        index,
        cards.length,
        selectedCards.contains(cards[index]),
      );
      minTop = top < minTop ? top : minTop;
      final bottom = top + cardHeight;
      maxBottom = bottom > maxBottom ? bottom : maxBottom;
    }

    final minOffset = -minTop;
    final maxOffset = fanHeight - maxBottom;
    if (maxOffset < minOffset) {
      return minOffset;
    }

    return maxOffset;
  }

  double _baseCardTop(int index, int count, bool isSelected) {
    final center = (count - 1) / 2;
    final distanceFromCenter = index - center;
    final normalized = center == 0 ? 0.0 : distanceFromCenter / center;
    final baseTop = 8 + normalized.abs() * 10 * arcScale;
    return isSelected ? baseTop - (18 * cardScale) : baseTop;
  }

  CardGeometry _cardGeometryAtIndex({
    required int index,
    required int count,
    required double startOffset,
    required double step,
    required double cardWidth,
    required double cardHeight,
    required double fanHeight,
    required double verticalAdjustment,
  }) {
    final center = (count - 1) / 2;
    final distanceFromCenter = index - center;
    final normalized = center == 0 ? 0.0 : distanceFromCenter / center;
    final angle = normalized * 0.12 * arcScale;
    final maxTop = fanHeight > cardHeight ? fanHeight - cardHeight : 0.0;
    final top =
        (_baseCardTop(index, count, selectedCards.contains(cards[index])) +
                verticalAdjustment)
            .clamp(0.0, maxTop)
            .toDouble();

    return CardGeometry(
      index: index,
      left: startOffset + index * step,
      top: top,
      width: cardWidth,
      height: cardHeight,
      angle: angle,
      visibleHitWidth: index == count - 1
          ? cardWidth
          : step.clamp(0.0, cardWidth).toDouble(),
    );
  }

  String? _cardAtOffset({
    required Offset localOffset,
    required double startOffset,
    required double step,
    required double cardWidth,
    required double cardHeight,
    required double fanHeight,
    required double verticalAdjustment,
  }) {
    if (cards.isEmpty) {
      return null;
    }

    for (var index = cards.length - 1; index >= 0; index--) {
      final geometry = _cardGeometryAtIndex(
        index: index,
        count: cards.length,
        startOffset: startOffset,
        step: step,
        cardWidth: cardWidth,
        cardHeight: cardHeight,
        fanHeight: fanHeight,
        verticalAdjustment: verticalAdjustment,
      );
      if (geometry.containsPointWithRotation(localOffset)) {
        return cards[index];
      }
    }

    return null;
  }

  Widget _fanHitBox({
    required double width,
    required double height,
    required Widget child,
  }) {
    final box = SizedBox(width: width, height: height, child: child);
    if (!showHitZoneOutline) {
      return box;
    }

    return DecoratedBox(
      position: DecorationPosition.foreground,
      decoration: BoxDecoration(
        border: Border.all(color: const Color(0xCC40C4FF), width: 2),
      ),
      child: box,
    );
  }

  Widget _positionedHandCard({
    required int index,
    required int count,
    required double step,
    required double startOffset,
    required double verticalAdjustment,
    required String label,
    required String displayLabel,
    required bool isPlayable,
    required double cardWidth,
    required double cardHeight,
    required double fanHeight,
  }) {
    final geometry = _cardGeometryAtIndex(
      index: index,
      count: count,
      startOffset: startOffset,
      step: step,
      cardWidth: cardWidth,
      cardHeight: cardHeight,
      fanHeight: fanHeight,
      verticalAdjustment: verticalAdjustment,
    );
    final isSelected = selectedCards.contains(label);

    return Positioned(
      left: geometry.left,
      top: geometry.top,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 160),
        curve: Curves.easeOutCubic,
        child: Transform.rotate(
          angle: geometry.angle,
          alignment: Alignment.bottomCenter,
          child: HandCard(
            label: displayLabel,
            isPlayable: isPlayable,
            isSelected: isSelected,
            showHitZoneOutline: showHitZoneOutline,
            key: ValueKey<String>('hand-$label'),
            scale: cardScale,
          ),
        ),
      ),
    );
  }
}
