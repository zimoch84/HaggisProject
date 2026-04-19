import 'package:flutter/material.dart';

import '../view_models/score_history_view_model.dart';

class ScoreHistoryPage extends StatelessWidget {
  const ScoreHistoryPage({
    super.key,
    required this.viewModel,
  });

  final ScoreHistoryViewModel viewModel;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Score History'),
      ),
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
                                const DataColumn(label: Text('Player')),
                                ...viewModel.roundNumbers.map(
                                  (int round) => DataColumn(label: Text('R$round')),
                                ),
                                const DataColumn(label: Text('Total')),
                              ],
                              rows: viewModel.players
                                  .map(
                                    (ScoreHistoryPlayerRowViewModel player) =>
                                        DataRow(
                                      cells: [
                                        DataCell(Text(player.playerId)),
                                        ...player.roundPoints.map(
                                          (int points) => DataCell(
                                            Text(points >= 0 ? '+$points' : '$points'),
                                          ),
                                        ),
                                        DataCell(Text(player.totalPoints.toString())),
                                      ],
                                    ),
                                  )
                                  .toList(growable: false),
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
