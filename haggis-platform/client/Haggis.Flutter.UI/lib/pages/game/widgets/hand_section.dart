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
    required this.onCreateGroup,
    required this.onClearGroup,
    required this.onGroupRainbowBomb,
    required this.onGroupColorBomb,
    required this.showCardHitZones,
    required this.cardScale,
    required this.spacingScale,
    required this.arcScale,
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
  final VoidCallback onCreateGroup;
  final VoidCallback onClearGroup;
  final VoidCallback? onGroupRainbowBomb;
  final VoidCallback? onGroupColorBomb;
  final bool showCardHitZones;
  final double cardScale;
  final double spacingScale;
  final double arcScale;
  final double verticalOffset;

  @override
  Widget build(BuildContext context) {
    return Stack(
      clipBehavior: Clip.none,
      children: [
        Positioned.fill(
          child: Padding(
            padding: const EdgeInsets.fromLTRB(8, 0, 8, 0),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                SizedBox(
                  width: savedGroup.isEmpty ? 0 : 176,
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
                            arcScale: arcScale,
                            verticalOffset: verticalOffset.clamp(-12.0, 36.0),
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
                            arcScale: arcScale,
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
          right: 0,
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              if (selectedCards.isNotEmpty)
                Padding(
                  padding: const EdgeInsets.only(right: 10),
                  child: FilledButton.tonal(
                    onPressed: onCreateGroup,
                    style: FilledButton.styleFrom(
                      foregroundColor: Colors.white,
                      backgroundColor: const Color(0xAA31544B),
                    ),
                    child: const Text('Grupuj zazn.'),
                  ),
                ),
              if (onGroupRainbowBomb != null)
                Padding(
                  padding: const EdgeInsets.only(right: 10),
                  child: FilledButton.tonalIcon(
                    onPressed: onGroupRainbowBomb,
                    icon: const Icon(Icons.auto_awesome_rounded, size: 18),
                    label: const Text('Rainbow'),
                    style: FilledButton.styleFrom(
                      foregroundColor: Colors.white,
                      backgroundColor: const Color(0xAA31544B),
                    ),
                  ),
                ),
              if (onGroupColorBomb != null)
                Padding(
                  padding: const EdgeInsets.only(right: 10),
                  child: FilledButton.tonalIcon(
                    onPressed: onGroupColorBomb,
                    icon: const Icon(Icons.palette_outlined, size: 18),
                    label: const Text('Kolor'),
                    style: FilledButton.styleFrom(
                      foregroundColor: Colors.white,
                      backgroundColor: const Color(0xAA31544B),
                    ),
                  ),
                ),
              if (savedGroup.isNotEmpty)
                Padding(
                  padding: const EdgeInsets.only(right: 10),
                  child: FilledButton.tonal(
                    onPressed: onClearGroup,
                    style: FilledButton.styleFrom(
                      foregroundColor: Colors.white,
                      backgroundColor: const Color(0xAA31544B),
                    ),
                    child: const Text('Reset grupy'),
                  ),
                ),
              HandSortToggle(
                handSortMode: handSortMode,
                onSortChanged: onSortChanged,
              ),
            ],
          ),
        ),
        if (savedGroup.isNotEmpty)
          const Positioned(
            left: 20,
            top: 10,
            child: Text(
              'Grupa',
              style: TextStyle(
                color: Colors.white70,
                fontWeight: FontWeight.w700,
              ),
            ),
          ),
      ],
    );
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
    required this.arcScale,
    required this.verticalOffset,
    required this.onCardScaleChanged,
    required this.onSpacingScaleChanged,
    required this.onArcScaleChanged,
    required this.onVerticalOffsetChanged,
    required this.showCardHitZones,
    required this.onShowCardHitZonesChanged,
  });

  final double cardScale;
  final double spacingScale;
  final double arcScale;
  final double verticalOffset;
  final ValueChanged<double> onCardScaleChanged;
  final ValueChanged<double> onSpacingScaleChanged;
  final ValueChanged<double> onArcScaleChanged;
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
            label: 'Luk wachlarza',
            value: arcScale,
            min: 0.7,
            max: 1.8,
            onChanged: onArcScaleChanged,
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
