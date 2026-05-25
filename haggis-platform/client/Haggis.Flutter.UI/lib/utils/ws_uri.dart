Uri buildWsUri(String baseUrl, String path) {
  final baseUri = Uri.parse(baseUrl);
  final scheme = baseUri.scheme == 'https' ? 'wss' : 'ws';
  return baseUri.replace(
    scheme: scheme,
    path: path,
    query: null,
    fragment: null,
  );
}
