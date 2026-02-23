import 'dart:io';

Future<void> main() async {
  const host = '127.0.0.1';
  const port = 8080;

  final server = await HttpServer.bind(host, port);
  stdout.writeln('WebSocket server running at ws://$host:$port');

  await for (final request in server) {
    if (request.uri.path != '/ws') {
      request.response
        ..statusCode = HttpStatus.notFound
        ..write('Use ws://$host:$port/ws');
      await request.response.close();
      continue;
    }

    final socket = await WebSocketTransformer.upgrade(request);
    socket.add('Hello, World!');

    await for (final message in socket) {
      socket.add('Server received: $message');
    }
  }
}
