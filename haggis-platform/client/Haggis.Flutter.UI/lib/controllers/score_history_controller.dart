import 'package:flutter/material.dart';

import '../view_models/score_history_view_model.dart';

class ScoreHistoryController extends ChangeNotifier {
  ScoreHistoryViewModel? _viewModel;

  ScoreHistoryViewModel? get viewModel => _viewModel;

  bool get hasData => _viewModel != null && _viewModel!.players.isNotEmpty;

  void setViewModel(ScoreHistoryViewModel viewModel) {
    _viewModel = viewModel;
    notifyListeners();
  }
}
