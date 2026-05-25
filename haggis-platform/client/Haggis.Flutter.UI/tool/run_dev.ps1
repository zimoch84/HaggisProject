param(
    [string]$Device = "all",
    [switch]$PrintOnly,
    [switch]$UseHash,
    [string[]]$FlutterArgs = @()
)

$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$buildInfoPath = Join-Path $projectRoot "lib/app/app_build_info.dart"
$buildInfo = Get-Content -LiteralPath $buildInfoPath -Raw
$markerMatch = [regex]::Match($buildInfo, "sourceMarker\s*=\s*'([^']+)'")
if (-not $markerMatch.Success) {
    throw "Could not read sourceMarker from $buildInfoPath"
}

$label = $markerMatch.Groups[1].Value

if ($UseHash) {
    $pathsToHash = @(
        "lib",
        "pubspec.yaml",
        "analysis_options.yaml",
        "assets/applicationsettings.json"
    )

    $sha = [System.Security.Cryptography.SHA256]::Create()
    $chunks = New-Object System.Collections.Generic.List[byte[]]

    foreach ($relativePath in $pathsToHash) {
        $path = Join-Path $projectRoot $relativePath
        if (-not (Test-Path -LiteralPath $path)) {
            continue
        }

        $files = if ((Get-Item -LiteralPath $path).PSIsContainer) {
            Get-ChildItem -LiteralPath $path -Recurse -File | Sort-Object FullName
        } else {
            @(Get-Item -LiteralPath $path)
        }

        foreach ($file in $files) {
            $relativeFile = $file.FullName.Substring($projectRoot.Length).TrimStart("\", "/").Replace("\", "/")
            $chunks.Add([System.Text.Encoding]::UTF8.GetBytes($relativeFile))
            $chunks.Add([byte[]](0))
            $chunks.Add([System.IO.File]::ReadAllBytes($file.FullName))
            $chunks.Add([byte[]](0))
        }
    }

    $totalLength = 0
    foreach ($chunk in $chunks) {
        $totalLength += $chunk.Length
    }

    $buffer = New-Object byte[] $totalLength
    $offset = 0
    foreach ($chunk in $chunks) {
        [System.Buffer]::BlockCopy($chunk, 0, $buffer, $offset, $chunk.Length)
        $offset += $chunk.Length
    }

    $hash = [System.BitConverter]::ToString($sha.ComputeHash($buffer)).Replace("-", "").Substring(0, 10).ToLowerInvariant()
    $label = "ui-$hash"
}

Write-Host "APP_BUILD_LABEL=$label"
if ($PrintOnly) {
    return
}

flutter run -d $Device "--dart-define=APP_BUILD_LABEL=$label" @FlutterArgs
