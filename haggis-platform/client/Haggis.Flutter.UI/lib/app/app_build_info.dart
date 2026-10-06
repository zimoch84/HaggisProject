class AppBuildInfo {
  AppBuildInfo._();

  static const sourceMarker = 'ui-20261006-029';
  static const buildLabel = String.fromEnvironment(
    'APP_BUILD_LABEL',
    defaultValue: sourceMarker,
  );

  static String get displayLabel => buildLabel;
}
