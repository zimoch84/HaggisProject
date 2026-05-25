import 'package:flutter/material.dart';

import '../view_models/round_over_view_model.dart';

class RoundOverController extends ChangeNotifier {
  RoundOverViewModel? _lastRound;
  RoundOverViewModel? _pendingRound;

  RoundOverViewModel? get lastRound => _lastRound;

  bool get hasLastRound => _lastRound != null;

  void setLastRound(RoundOverViewModel viewModel) {
    _lastRound = viewModel;
    _pendingRound = viewModel;
    notifyListeners();
  }

  RoundOverViewModel? consumePendingRound() {
    final round = _pendingRound;
    _pendingRound = null;
    return round;
  }
}
