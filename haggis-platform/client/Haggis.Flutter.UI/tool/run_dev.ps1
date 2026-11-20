param(
    [string]$Device = "all",
    [switch]$PrintOnly,
    [switch]$UseHash,
    [string[]]$FlutterArgs = @()
)

$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$pubspecPath = Join-Path $projectRoot "pubspec.yaml"
$pubspec = Get-Content -LiteralPath $pubspecPath -Raw
$versionMatch = [regex]::Match($pubspec, '(?m)^version: +(\d{4})\.(\d{1,2})\.(\d{1,2})\+(\d+)\s*$')
if (-not $versionMatch.Success) {
    throw "Expected version: YYYY.M.D+build in $pubspecPath"
}
$label = 'ui-{0}{1:D2}{2:D2}-{3:D3}' -f $versionMatch.Groups[1].Value, [int]$versionMatch.Groups[2].Value, [int]$versionMatch.Groups[3].Value, [int]$versionMatch.Groups[4].Value

if ($UseHash) {
    throw "UseHash is no longer supported; set the version in pubspec.yaml."
}

Write-Host "UI version=$label"
if ($PrintOnly) {
    return
}

flutter run -d $Device @FlutterArgs
