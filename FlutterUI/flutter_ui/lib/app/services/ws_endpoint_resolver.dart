import 'package:flutter/foundation.dart';

class WsEndpointResolver {
  const WsEndpointResolver();

  String hostForCurrentPlatform(String host) {
    if (kIsWeb) {
      return host;
    }
    if (defaultTargetPlatform == TargetPlatform.android &&
        (host == 'localhost' || host == '127.0.0.1')) {
      return '10.0.2.2';
    }
    return host;
  }
}
