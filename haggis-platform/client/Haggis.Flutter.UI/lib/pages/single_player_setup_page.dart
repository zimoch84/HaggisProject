import 'package:flutter/material.dart';

import '../models/single_player_models.dart';

class SinglePlayerSetupPage extends StatefulWidget {
  const SinglePlayerSetupPage({
    super.key,
    required this.playerId,
    required this.onStart,
    required this.onBack,
  });

  final String playerId;
  final Future<void> Function(List<SinglePlayerAiConfig> aiPlayers) onStart;
  final VoidCallback onBack;

  @override
  State<SinglePlayerSetupPage> createState() => _SinglePlayerSetupPageState();
}

class _SinglePlayerSetupPageState extends State<SinglePlayerSetupPage> {
  final List<SinglePlayerAiConfig> _aiPlayers = <SinglePlayerAiConfig>[
    const SinglePlayerAiConfig(name: 'AI-1', difficulty: AiDifficulty.normal),
    const SinglePlayerAiConfig(name: 'AI-2', difficulty: AiDifficulty.normal),
  ];
  bool _starting = false;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Single Player: ${widget.playerId}'),
        leading: IconButton(
          tooltip: 'Back',
          onPressed: _starting ? null : widget.onBack,
          icon: const Icon(Icons.arrow_back),
        ),
      ),
      body: SafeArea(
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 720),
            child: SingleChildScrollView(
              padding: const EdgeInsets.all(24),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(
                    'AI players',
                    style: Theme.of(context).textTheme.headlineSmall,
                  ),
                  const SizedBox(height: 8),
                  Text(
                    'Haggis supports 2 or 3 total players, so Single Player can use 1 or 2 AI opponents.',
                    style: Theme.of(context).textTheme.bodyMedium,
                  ),
                  const SizedBox(height: 20),
                  for (var index = 0; index < _aiPlayers.length; index++) ...[
                    _AiConfigTile(
                      config: _aiPlayers[index],
                      canRemove: _aiPlayers.length > 1,
                      onDifficultyChanged: (AiDifficulty difficulty) {
                        setState(() {
                          _aiPlayers[index] = _aiPlayers[index].copyWith(
                            difficulty: difficulty,
                          );
                        });
                      },
                      onRemove: () {
                        setState(() {
                          _aiPlayers.removeAt(index);
                        });
                      },
                    ),
                    const SizedBox(height: 12),
                  ],
                  OutlinedButton.icon(
                    onPressed: _aiPlayers.length >= 2 || _starting
                        ? null
                        : _addAi,
                    icon: const Icon(Icons.add),
                    label: const Text('Add AI'),
                  ),
                  const SizedBox(height: 16),
                  FilledButton(
                    onPressed: _starting ? null : _start,
                    child: Text(
                      _starting ? 'Starting...' : 'Start Single Player',
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }

  void _addAi() {
    setState(() {
      _aiPlayers.add(
        SinglePlayerAiConfig(
          name: 'AI-${_aiPlayers.length + 1}',
          difficulty: AiDifficulty.normal,
        ),
      );
    });
  }

  Future<void> _start() async {
    setState(() {
      _starting = true;
    });
    try {
      await widget.onStart(List<SinglePlayerAiConfig>.unmodifiable(_aiPlayers));
    } finally {
      if (mounted) {
        setState(() {
          _starting = false;
        });
      }
    }
  }
}

class _AiConfigTile extends StatelessWidget {
  const _AiConfigTile({
    required this.config,
    required this.canRemove,
    required this.onDifficultyChanged,
    required this.onRemove,
  });

  final SinglePlayerAiConfig config;
  final bool canRemove;
  final ValueChanged<AiDifficulty> onDifficultyChanged;
  final VoidCallback onRemove;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Row(
          children: [
            const Icon(Icons.smart_toy),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    config.name,
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                  Text(config.difficulty.description),
                ],
              ),
            ),
            DropdownButton<AiDifficulty>(
              value: config.difficulty,
              onChanged: (AiDifficulty? value) {
                if (value != null) {
                  onDifficultyChanged(value);
                }
              },
              items: AiDifficulty.values
                  .map(
                    (AiDifficulty difficulty) => DropdownMenuItem<AiDifficulty>(
                      value: difficulty,
                      child: Text('${difficulty.value}. ${difficulty.label}'),
                    ),
                  )
                  .toList(growable: false),
            ),
            IconButton(
              tooltip: 'Remove AI',
              onPressed: canRemove ? onRemove : null,
              icon: const Icon(Icons.remove_circle_outline),
            ),
          ],
        ),
      ),
    );
  }
}
