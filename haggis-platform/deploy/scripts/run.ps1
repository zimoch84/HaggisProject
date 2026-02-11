param([switch]$Build)
$ErrorActionPreference = "Stop"
if ($Build) {
  docker compose up -d --build
} else {
  docker compose up -d
}
Write-Host "API: http://localhost:8080" -ForegroundColor Cyan
Write-Host "WebSocket: ws://localhost:8080" -ForegroundColor Cyan
