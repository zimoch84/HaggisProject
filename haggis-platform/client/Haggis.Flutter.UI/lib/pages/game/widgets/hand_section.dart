import 'package:flutter/material.dart';

import '../models/card_ui_models.dart';
import 'cards.dart';

class HandSection extends StatelessWidget {
  const HandSection({
    super.key,
    required this.cards,
    required this.savedGroup,
    required this.playableCards,
    required this.selectedCards,
    required this.cardLabelBuilder,
    required this.onCardTap,
    required this.handSortMode,
    required this.onSortChanged,
    required this.onGroupColorBomb,
    this.onCreateGroup,
    this.onClearGroup,
    this.onGroupRainbowBomb,
    required this.showCardHitZones,
    required this.cardScale,
    required this.spacingScale,
    required this.verticalOffset,
  });

  final List<String> cards;
  final List<String> savedGroup;
  final Set<String> playableCards;
  final List<String> selectedCards;
  final String Function(String card) cardLabelBuilder;
  final Future<void> Function(String card) onCardTap;
  final HandSortMode handSortMode;
  final ValueChanged<HandSortMode> onSortChanged;
  final VoidCallback? onGroupColorBomb;
  final VoidCallback? onCreateGroup;
  final VoidCallback? onClearGroup;
  final VoidCallback? onGroupRainbowBomb;
  final bool showCardHitZones;
  final double cardScale;
  final double spacingScale;
  final double verticalOffset;

  @override
  Widget build(BuildContext context) {
    final groupButtonStyle = FilledButton.styleFrom(
      backgroundColor: const Color(0x66163034),
      foregroundColor: Colors.white,
      disabledBackgroundColor: const Color(0x33163034),
      disabledForegroundColor: Colors.white38,
      side: const BorderSide(color: Color(0x6656B891)),
      shape: const StadiumBorder(),
    );
    return LayoutBuilder(
      builder: (BuildContext context, BoxConstraints constraints) {
        final savedGroupWidth = _resolveSavedGroupWidth(constraints.maxWidth);

        return Stack(
          clipBehavior: Clip.none,
          children: [
            Positioned.fill(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(8, 92, 8, 4),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    SizedBox(
                      width: savedGroupWidth,
                      child: Align(
                        alignment: Alignment.bottomCenter,
                        child: savedGroup.isEmpty
                            ? const SizedBox.shrink()
                            : PlayerHandFan(
                                cards: savedGroup,
                                playableCards: playableCards,
                                selectedCards: selectedCards,
                                cardLabelBuilder: cardLabelBuilder,
                                onCardTap: onCardTap,
                                showHitZoneOutline: showCardHitZones,
                                cardScale: cardScale * 0.92,
                                spacingScale: spacingScale,
                                verticalOffset: verticalOffset.clamp(
                                  -12.0,
                                  36.0,
                                ),
                              ),
                      ),
                    ),
                    SizedBox(width: savedGroup.isEmpty ? 0 : 12),
                    Expanded(
                      child: Align(
                        alignment: Alignment.bottomLeft,
                        child: cards.isEmpty
                            ? const Center(
                                child: Text(
                                  'Brak kart na rece',
                                  style: TextStyle(
                                    color: Colors.white70,
                                    shadows: [
                                      Shadow(
                                        color: Color(0xCC000000),
                                        blurRadius: 8,
                                      ),
                                    ],
                                  ),
                                ),
                              )
                            : PlayerHandFan(
                                cards: cards,
                                playableCards: playableCards,
                                selectedCards: selectedCards,
                                cardLabelBuilder: cardLabelBuilder,
                                onCardTap: onCardTap,
                                showHitZoneOutline: showCardHitZones,
                                cardScale: cardScale,
                                spacingScale: spacingScale,
                                verticalOffset: verticalOffset,
                              ),
                      ),
                    ),
                  ],
                ),
              ),
            ),
            Positioned(
              top: 0,
              left: 8,
              right: 0,
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    child: SingleChildScrollView(
                      scrollDirection: Axis.horizontal,
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          SizedBox(
                            height: 48,
                            child: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                if (onCreateGroup != null &&
                                    selectedCards.any(
                                      (card) => !savedGroup.contains(card),
                                    ))
                                  FilledButton.tonal(
                                    style: groupButtonStyle,
                                    onPressed: onCreateGroup,
                                    child: const Text('Group'),
                                  ),
                              ],
                            ),
                          ),
                          SizedBox(
                            height: 48,
                            child: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                if (savedGroup.isNotEmpty)
                                  FilledButton.tonal(
                                    style: groupButtonStyle,
                                    onPressed: onClearGroup,
                                    child: const Text('Reset group'),
                                  )
                                else ...[
                                  if (onGroupRainbowBomb != null)
                                    FilledButton.tonal(
                                      style: groupButtonStyle,
                                      onPressed: onGroupRainbowBomb,
                                      child: const Text('Rainbow'),
                                    ),
                                  if (onGroupRainbowBomb != null &&
                                      onGroupColorBomb != null)
                                    const SizedBox(width: 6),
                                  if (onGroupColorBomb != null)
                                    FilledButton.tonal(
                                      style: groupButtonStyle,
                                      onPressed: onGroupColorBomb,
                                      child: const Text('Color (bomb)'),
                                    ),
                                ],
                              ],
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(width: 8),
                  HandSortToggle(
                    handSortMode: handSortMode,
                    onSortChanged: onSortChanged,
                  ),
                ],
              ),
            ),
          ],
        );
      },
    );
  }

  double _resolveSavedGroupWidth(double availableWidth) {
    if (savedGroup.isEmpty) {
      return 0;
    }

    final savedGroupCardScale = cardScale * 0.92;
    final cardWidth = 62.0 * savedGroupCardScale;
    final preferredStep = 32.0 * spacingScale;
    final desiredWidth = cardWidth + ((savedGroup.length - 1) * preferredStep);
    const horizontalPadding = 16.0;
    const gapToMainHand = 12.0;

    if (!availableWidth.isFinite) {
      return desiredWidth + horizontalPadding;
    }

    final maxWidth =
        ((availableWidth - horizontalPadding - gapToMainHand) *
                (cards.isEmpty ? 1.0 : 0.4))
            .clamp(0.0, double.infinity);
    return (desiredWidth + horizontalPadding).clamp(0.0, maxWidth);
  }
}

class HandSortToggle extends StatelessWidget {
  const HandSortToggle({
    super.key,
    required this.handSortMode,
    required this.onSortChanged,
  });

  final HandSortMode handSortMode;
  final ValueChanged<HandSortMode> onSortChanged;

  @override
  Widget build(BuildContext context) {
    return SegmentedButton<HandSortMode>(
      showSelectedIcon: false,
      style: ButtonStyle(
        backgroundColor: WidgetStateProperty.resolveWith((
          Set<WidgetState> states,
        ) {
          if (states.contains(WidgetState.selected)) {
            return const Color(0xFFB15D45);
          }
          return const Color(0x33162A2E);
        }),
        foregroundColor: WidgetStateProperty.all<Color>(Colors.white),
        side: WidgetStateProperty.all(
          const BorderSide(color: Color(0x6656B891)),
        ),
        visualDensity: VisualDensity.compact,
      ),
      segments: const [
        ButtonSegment<HandSortMode>(
          value: HandSortMode.rank,
          label: Text('Starsz.'),
        ),
        ButtonSegment<HandSortMode>(
          value: HandSortMode.color,
          label: Text('Kolor'),
        ),
      ],
      selected: <HandSortMode>{handSortMode},
      onSelectionChanged: (Set<HandSortMode> selection) {
        if (selection.isEmpty) {
          return;
        }
        onSortChanged(selection.first);
      },
    );
  }
}

class HandTuningPanel extends StatelessWidget {
  const HandTuningPanel({
    super.key,
    required this.cardScale,
    required this.spacingScale,
    required this.verticalOffset,
    required this.onCardScaleChanged,
    required this.onSpacingScaleChanged,
    required this.onVerticalOffsetChanged,
    required this.showCardHitZones,
    required this.onShowCardHitZonesChanged,
  });

  final double cardScale;
  final double spacingScale;
  final double verticalOffset;
  final ValueChanged<double> onCardScaleChanged;
  final ValueChanged<double> onSpacingScaleChanged;
  final ValueChanged<double> onVerticalOffsetChanged;
  final bool showCardHitZones;
  final ValueChanged<bool> onShowCardHitZonesChanged;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(top: 6),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          SwitchListTile.adaptive(
            contentPadding: EdgeInsets.zero,
            title: const Text(
              'Pokaz hitbox',
              style: TextStyle(
                color: Colors.white,
                fontWeight: FontWeight.w600,
              ),
            ),
            value: showCardHitZones,
            onChanged: onShowCardHitZonesChanged,
          ),
          HandTuningSlider(
            label: 'Przesuniecie gora/dol',
            value: verticalOffset,
            min: -24,
            max: 96,
            onChanged: onVerticalOffsetChanged,
          ),
          HandTuningSlider(
            label: 'Odstep kart',
            value: spacingScale,
            min: 0.7,
            max: 1.6,
            onChanged: onSpacingScaleChanged,
          ),
          HandTuningSlider(
            label: 'Skala kart',
            value: cardScale,
            min: 0.9,
            max: 1.8,
            onChanged: onCardScaleChanged,
          ),
        ],
      ),
    );
  }
}

class HandTuningSlider extends StatelessWidget {
  const HandTuningSlider({
    super.key,
    required this.label,
    required this.value,
    required this.min,
    required this.max,
    required this.onChanged,
  });

  final String label;
  final double value;
  final double min;
  final double max;
  final ValueChanged<double> onChanged;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        SizedBox(
          width: 130,
          child: Text(
            label,
            style: const TextStyle(
              color: Colors.white,
              fontWeight: FontWeight.w600,
            ),
          ),
        ),
        Expanded(
          child: Slider(value: value, min: min, max: max, onChanged: onChanged),
        ),
        SizedBox(
          width: 52,
          child: Text(
            value.toStringAsFixed(2),
            textAlign: TextAlign.right,
            style: const TextStyle(color: Colors.white70),
          ),
        ),
      ],
    );
  }
}
