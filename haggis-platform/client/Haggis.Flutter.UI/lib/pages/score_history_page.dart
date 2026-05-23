import 'package:flutter/material.dart';

import '../view_models/score_history_view_model.dart';

class ScoreHistoryPage extends StatelessWidget {
  const ScoreHistoryPage({super.key, required this.viewModel});

  final ScoreHistoryViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Score History')),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text('Game: ${viewModel.gameId}'),
                const SizedBox(height: 12),
                Expanded(
                  child: viewModel.players.isEmpty
                      ? const Center(child: Text('(no score history yet)'))
                      : SingleChildScrollView(
                          scrollDirection: Axis.horizontal,
                          child: SingleChildScrollView(
                            child: DataTable(
                              columns: [
                                const DataColumn(label: Text('Round')),
                                ...viewModel.players.map(
                                  (ScoreHistoryPlayerRowViewModel player) =>
                                      DataColumn(label: Text(player.playerId)),
                                ),
                                const DataColumn(label: Text('Total')),
                              ],
                              rows: [
                                ...viewModel.roundNumbers.asMap().entries.map(
                                  (MapEntry<int, int> round) => DataRow(
                                    cells: [
                                      DataCell(Text('R${round.value}')),
                                      ...viewModel.players.map((
                                        ScoreHistoryPlayerRowViewModel player,
                                      ) {
                                        final points =
                                            round.key <
                                                player.roundPoints.length
                                            ? player.roundPoints[round.key]
                                            : 0;
                                        return DataCell(
                                          Text(_formatSigned(points)),
                                        );
                                      }),
                                      DataCell(
                                        Text(_formatRoundTotal(round.key)),
                                      ),
                                    ],
                                  ),
                                ),
                                DataRow(
                                  cells: [
                                    const DataCell(Text('Total')),
                                    ...viewModel.players.map(
                                      (ScoreHistoryPlayerRowViewModel player) =>
                                          DataCell(
                                            Text(player.totalPoints.toString()),
                                          ),
                                    ),
                                    DataCell(Text(_formatGrandTotal())),
                                  ],
                                ),
                              ],
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

  String _formatRoundTotal(int roundIndex) {
    final total = viewModel.players.fold<int>(0, (
      int sum,
      ScoreHistoryPlayerRowViewModel player,
    ) {
      if (roundIndex >= player.roundPoints.length) {
        return sum;
      }

      return sum + player.roundPoints[roundIndex];
    });
    return _formatSigned(total);
  }

  String _formatGrandTotal() {
    final total = viewModel.players.fold<int>(
      0,
      (int sum, ScoreHistoryPlayerRowViewModel player) =>
          sum + player.totalPoints,
    );
    return total.toString();
  }

  String _formatSigned(int points) => points >= 0 ? '+$points' : '$points';
}
