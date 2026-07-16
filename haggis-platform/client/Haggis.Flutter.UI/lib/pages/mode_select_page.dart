import 'package:flutter/material.dart';

class ModeSelectPage extends StatelessWidget {
  const ModeSelectPage({
    super.key,
    required this.playerId,
    required this.onSinglePlayer,
    required this.onMultiPlayer,
    required this.onDisconnect,
  });

  final String playerId;
  final VoidCallback onSinglePlayer;
  final Future<void> Function() onMultiPlayer;
  final VoidCallback onDisconnect;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Choose mode: $playerId'),
        actions: [
          IconButton(
            tooltip: 'Log out',
            onPressed: onDisconnect,
            icon: const Icon(Icons.logout),
          ),
        ],
      ),
      body: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 720),
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Row(
              children: [
                Expanded(
                  child: _ModeCard(
                    title: 'Single Player',
                    description: 'Play against two AI players.',
                    icon: Icons.person,
                    onPressed: onSinglePlayer,
                  ),
                ),
                const SizedBox(width: 20),
                Expanded(
                  child: _ModeCard(
                    title: 'MultiPlayer',
                    description: 'Open lobby, create rooms, and play online.',
                    icon: Icons.groups,
                    onPressed: () => onMultiPlayer(),
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

class _ModeCard extends StatelessWidget {
  const _ModeCard({
    required this.title,
    required this.description,
    required this.icon,
    required this.onPressed,
  });

  final String title;
  final String description;
  final IconData icon;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: onPressed,
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(icon, size: 48),
              const SizedBox(height: 16),
              Text(title, style: Theme.of(context).textTheme.headlineSmall),
              const SizedBox(height: 8),
              Text(
                description,
                textAlign: TextAlign.center,
                style: Theme.of(context).textTheme.bodyMedium,
              ),
              const SizedBox(height: 20),
              FilledButton(onPressed: onPressed, child: const Text('Select')),
            ],
          ),
        ),
      ),
    );
  }
}
