# Haggis.Flutter.UI

## Wydanie Android APK

Wersję ustawiaj tylko w `pubspec.yaml`, np. `version: 2026.10.7+30`. Końcowy numer jest wspólnym numerem porządkowym wydań
i Android `versionCode` (030 = 30); zwiększaj go dla każdego nowego wydania,
bez resetowania po zmianie daty. Skrypt nie zmienia wersji automatycznie.

```bash
python3 tool/release_android.py --dry-run
python3 tool/release_android.py
python3 tool/release_android.py --drive 'gdrive:HaggisRelease'
```

Aplikacja odczytuje wersję z `pubspec.yaml` dołączonego jako zasób, zarówno
w debug, jak i release. Nie trzeba ustawiać `APP_BUILD_LABEL`.
Ekran pokazuje `ui-20261007-030`. Android otrzymuje `versionCode=30`
i `versionName=2026.10.7`.
Plik wynikowy to `build/releases/haggis-game-20261007-030.apk`.
Parametry builda nadpisują wersję z `pubspec.yaml`.

W VS Code wybierz **Terminal → Run Task → Haggis: Build Android APK** lub
**Haggis: Deploy Android to Google Drive**. Zadanie deploy wysyła do folderu `HaggisRelease` przez połączenie `gdrive`.
Można też przekazać folder przez zmienną `HAGGIS_DRIVE_FOLDER` przy uruchamianiu
skryptu z terminala. Wymagane: Python 3, Flutter, Android SDK/JDK, a do wysyłki
również `rclone` skonfigurowany przez `rclone config` z dostępem do Google Drive.
Nazwa `gdrive` w przykładzie musi odpowiadać nazwie połączenia w rclone.
Połączenie Drive w czacie nie konfiguruje rclone na komputerze.
Wysyłka nie nadpisuje innego pliku o tej samej nazwie (`--immutable`).

Aktualna konfiguracja Gradle podpisuje release kluczem debugowym.
Przed dystrybucją regularnych wydań skonfiguruj stały klucz release, zachowaj
jego kopię i przechowuj go oraz hasła poza repozytorium.

A new Flutter project.

## Getting Started

This project is a starting point for a Flutter application.

A few resources to get you started if this is your first Flutter project:

- [Learn Flutter](https://docs.flutter.dev/get-started/learn-flutter)
- [Write your first Flutter app](https://docs.flutter.dev/get-started/codelab)
- [Flutter learning resources](https://docs.flutter.dev/reference/learning-resources)

For help getting started with Flutter development, view the
[online documentation](https://docs.flutter.dev/), which offers tutorials,
samples, guidance on mobile development, and a full API reference.

W panelu **Run and Debug** wybierz **Haggis: Build Android APK** albo
**Haggis: Deploy Android to Google Drive** i kliknij ▶ (F5).
Konfiguracje uruchamiają skrypt w terminalu przez wbudowany JavaScript Debugger VS Code.
