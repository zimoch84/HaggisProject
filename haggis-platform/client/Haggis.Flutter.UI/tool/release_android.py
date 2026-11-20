#!/usr/bin/env python3
"""Build the version recorded in pubspec.yaml, optionally upload to Drive."""
import argparse
import os
from pathlib import Path
import re
import shutil
import subprocess


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--drive', help='rclone destination folder, e.g. gdrive:HaggisRelease')
    parser.add_argument('--dry-run', action='store_true', help='Print version and commands without building/uploading')
    args = parser.parse_args()
    root = Path(__file__).resolve().parent.parent
    source = root / 'pubspec.yaml'
    match = re.search(r'^version: +(\d{4})\.(\d{1,2})\.(\d{1,2})\+(\d+)\s*$', source.read_text(), re.MULTILINE)
    if not match:
        parser.error(f'Expected version: YYYY.M.D+build in {source}')
    year, month, day, build = map(int, match.groups())
    from datetime import datetime
    try:
        date = datetime(year, month, day).strftime('%Y%m%d')
    except ValueError:
        parser.error('Invalid date in pubspec version')
    number = build
    if not 1 <= number <= 2100000000:
        parser.error('Build number must be between 1 and 2100000000')
    build = f'{number:03d}'
    label = f'ui-{date}-{build}'
    filename = f'haggis-game-{date}-{build}.apk'
    destination = args.drive or os.environ.get('HAGGIS_DRIVE_FOLDER')
    if destination and ':' not in destination:
        parser.error('Drive folder must be an rclone path, e.g. gdrive:HaggisRelease')
    command = ['flutter', 'build', 'apk', '--release',
               f'--build-name={year}.{month}.{day}', f'--build-number={number}']
    output = root / 'build/releases' / filename
    print(f'UI version={label}\nversionCode={number}\nAPK={output}', flush=True)
    print(' '.join(command), flush=True)
    if destination:
        print(f'Upload: {destination.rstrip("/")}/{filename}', flush=True)
    if args.dry_run:
        return
    for executable in ['flutter'] + (['rclone'] if destination else []):
        if not shutil.which(executable):
            parser.error(f'{executable} is not available in PATH')
    subprocess.run(command, cwd=root, check=True)
    output.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(root / 'build/app/outputs/flutter-apk/app-release.apk', output)
    if destination:
        subprocess.run(['rclone', 'copyto', str(output),
                        f'{destination.rstrip("/")}/{filename}', '--immutable'], check=True)
    print(f'Done: {output}', flush=True)


if __name__ == '__main__':
    main()
