class SinglePlayerAiConfig {
  const SinglePlayerAiConfig({required this.name, required this.difficulty});

  final String name;
  final AiDifficulty difficulty;

  SinglePlayerAiConfig copyWith({String? name, AiDifficulty? difficulty}) {
    return SinglePlayerAiConfig(
      name: name ?? this.name,
      difficulty: difficulty ?? this.difficulty,
    );
  }
}

enum AiDifficulty {
  easy(1, 'Easy', 'Random moves'),
  normal(2, 'Normal', 'Heuristic'),
  medium(3, 'Medium', 'Monte Carlo'),
  hard(4, 'Hard', 'Monte Carlo 2000/2000'),
  expert(5, 'Expert', 'Monte Carlo 2000/2000 + heuristic 5/3');

  const AiDifficulty(this.value, this.label, this.description);

  final int value;
  final String label;
  final String description;
}
