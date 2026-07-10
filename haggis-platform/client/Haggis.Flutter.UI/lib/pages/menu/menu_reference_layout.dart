import 'dart:ui';

class MenuReferenceLayout {
  const MenuReferenceLayout._();

  static const Size referenceSize = Size(1672, 941);

  static Rect rect({
    required double x,
    required double y,
    required double width,
    required double height,
  }) {
    return Rect.fromLTWH(x, y, width, height);
  }

  static Rect scaleRect(Rect rect, Size targetSize) {
    final scaleX = targetSize.width / referenceSize.width;
    final scaleY = targetSize.height / referenceSize.height;

    return Rect.fromLTWH(
      rect.left * scaleX,
      rect.top * scaleY,
      rect.width * scaleX,
      rect.height * scaleY,
    );
  }
}
