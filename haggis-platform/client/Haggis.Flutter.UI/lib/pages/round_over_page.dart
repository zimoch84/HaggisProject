import 'package:flutter/material.dart';

import '../view_models/round_over_view_model.dart';

class RoundOverPage extends StatelessWidget {
  const RoundOverPage({
    super.key,
    required this.viewModel,
  });

  final RoundOverViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Round ${viewModel.roundNumber}'),
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Round Over', style: Theme.of(context).textTheme.titleLarge),
                  const SizedBox(height: 12),
                  Text('Game: ${viewModel.gameId}'),
                  Text('Round finished: ${viewModel.roundNumber}'),
                  Text('Next round: ${viewModel.nextRoundNumber}'),
                  Text(
                    'Winner: ${viewModel.winnerPlayerId.isEmpty ? '(unknown)' : viewModel.winnerPlayerId}',
                  ),
                  const SizedBox(height: 8),
                  Text(viewModel.status),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text('Score Table', style: Theme.of(context).textTheme.titleLarge),
                  const SizedBox(height: 12),
                  SingleChildScrollView(
                    scrollDirection: Axis.horizontal,
                    child: DataTable(
                      columns: const [
                        DataColumn(label: Text('Player')),
                        DataColumn(label: Text('Wzietki')),
                        DataColumn(label: Text('Pozost')),
                        DataColumn(label: Text('Haggis')),
                        DataColumn(label: Text('Runda')),
                        DataColumn(label: Text('Total')),
                      ],
                      rows: viewModel.players
                          .map(
                            (RoundOverPlayerRowViewModel player) => DataRow(
                              cells: [
                                DataCell(Text(player.playerId)),
                                DataCell(Text(_formatSigned(player.tricksPoints))),
                                DataCell(
                                  Text(
                                    _formatSigned(
                                      player.opponentsRemainingCardsPoints,
                                    ),
                                  ),
                                ),
                                DataCell(Text(_formatSigned(player.haggisPoints))),
                                DataCell(Text(_formatSigned(player.roundPoints))),
                                DataCell(Text(player.totalPoints.toString())),
                              ],
                            ),
                          )
                          .toList(growable: false),
                    ),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Haggis', style: Theme.of(context).textTheme.titleLarge),
                  const SizedBox(height: 12),
                  Text(
                    viewModel.haggisCards.isEmpty
                        ? '(no haggis cards)'
                        : viewModel.haggisCards.join(' '),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Last Sequence',
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                  const SizedBox(height: 12),
                  if (viewModel.lastSequenceLines.isEmpty)
                    const Text('(no moves captured)')
                  else
                    ...viewModel.lastSequenceLines.map(Text.new),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  static String _formatSigned(int value) {
    return value >= 0 ? '+$value' : value.toString();
  }
}
