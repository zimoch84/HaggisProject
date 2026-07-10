param(
  [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot ".."))
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

$assetsRoot = Join-Path $ProjectRoot "assets"
$outputRoot = Join-Path $assetsRoot "ui\menu"
New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null

$referenceWidth = 1672
$referenceHeight = 941

$slices = @(
  @{
    Source = "haggis_name_single_multi.png"
    Items = @(
      @{ Name = "name_screen_background"; X = 0; Y = 0; W = 1672; H = 941; Mask = "none" },
      @{ Name = "name_screen_panel"; X = 374; Y = 48; W = 916; H = 840; Mask = "panel" },
      @{ Name = "name_input_frame"; X = 546; Y = 456; W = 568; H = 88; Mask = "input" },
      @{ Name = "button_single_player"; X = 536; Y = 573; W = 598; H = 105; Mask = "button" },
      @{ Name = "button_multiplayer"; X = 536; Y = 699; W = 598; H = 106; Mask = "button" }
    )
  },
  @{
    Source = "singleplayer_choose_ai.png"
    Items = @(
      @{ Name = "choose_ai_background"; X = 0; Y = 0; W = 1672; H = 941; Mask = "none" },
      @{ Name = "choose_ai_panel"; X = 373; Y = 49; W = 918; H = 842; Mask = "panel" },
      @{ Name = "ai_opponent1_random"; X = 452; Y = 432; W = 176; H = 84; Mask = "option" },
      @{ Name = "ai_opponent1_normal"; X = 646; Y = 432; W = 193; H = 84; Mask = "option" },
      @{ Name = "ai_opponent1_heuristic"; X = 856; Y = 432; W = 172; H = 84; Mask = "option" },
      @{ Name = "ai_opponent1_montecarlo"; X = 1048; Y = 432; W = 191; H = 84; Mask = "option" },
      @{ Name = "ai_opponent2_none"; X = 405; Y = 602; W = 171; H = 84; Mask = "option" },
      @{ Name = "ai_opponent2_random"; X = 590; Y = 602; W = 160; H = 84; Mask = "option" },
      @{ Name = "ai_opponent2_normal"; X = 768; Y = 602; W = 160; H = 84; Mask = "option" },
      @{ Name = "ai_opponent2_heuristic"; X = 943; Y = 602; W = 160; H = 84; Mask = "option" },
      @{ Name = "ai_opponent2_montecarlo"; X = 1116; Y = 602; W = 174; H = 84; Mask = "option" },
      @{ Name = "button_start_game"; X = 584; Y = 716; W = 520; H = 95; Mask = "button" },
      @{ Name = "button_back"; X = 710; Y = 825; W = 273; H = 59; Mask = "buttonSmall" },
      @{ Name = "selected_check"; X = 719; Y = 416; W = 42; H = 42; Mask = "check" }
    )
  }
)

function Get-RoundedMaskRadius {
  param(
    [string]$Mask
  )

  switch ($Mask) {
    "panel" { return 30 }
    "input" { return 18 }
    "button" { return 24 }
    "buttonSmall" { return 15 }
    "option" { return 16 }
    default { return 0 }
  }
}

function Get-RoundedRectAlpha {
  param(
    [int]$X,
    [int]$Y,
    [int]$Width,
    [int]$Height,
    [int]$Radius
  )

  if ($Radius -le 0) {
    return 255
  }

  $leftCenter = $Radius - 0.5
  $rightCenter = $Width - $Radius - 0.5
  $topCenter = $Radius - 0.5
  $bottomCenter = $Height - $Radius - 0.5
  $cx = [Math]::Min([Math]::Max($X, $leftCenter), $rightCenter)
  $cy = [Math]::Min([Math]::Max($Y, $topCenter), $bottomCenter)
  $dx = $X - $cx
  $dy = $Y - $cy
  $distance = [Math]::Sqrt(($dx * $dx) + ($dy * $dy))

  if ($distance -le ($Radius - 1)) {
    return 255
  }
  if ($distance -ge $Radius) {
    return 0
  }

  return [int][Math]::Round(($Radius - $distance) * 255)
}

function Get-CheckMaskAlpha {
  param(
    [System.Drawing.Color]$Color
  )

  $brightness = (($Color.R * 0.299) + ($Color.G * 0.587) + ($Color.B * 0.114))
  if ($brightness -le 95) {
    return 255
  }
  if ($brightness -ge 135) {
    return 0
  }

  return [int][Math]::Round((135 - $brightness) / 40 * 255)
}

function Apply-SliceMask {
  param(
    [System.Drawing.Bitmap]$Bitmap,
    [hashtable]$Slice
  )

  $mask = if ($Slice.ContainsKey("Mask")) { $Slice.Mask } else { "none" }
  if ($mask -eq "none") {
    return
  }

  $radius = Get-RoundedMaskRadius -Mask $mask
  for ($y = 0; $y -lt $Bitmap.Height; $y++) {
    for ($x = 0; $x -lt $Bitmap.Width; $x++) {
      $color = $Bitmap.GetPixel($x, $y)
      if ($mask -eq "check") {
        $alpha = Get-CheckMaskAlpha -Color $color
      }
      else {
        $alpha = Get-RoundedRectAlpha -X $x -Y $y -Width $Bitmap.Width -Height $Bitmap.Height -Radius $radius
      }

      if ($alpha -ne $color.A) {
        $Bitmap.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($alpha, $color.R, $color.G, $color.B))
      }
    }
  }
}

function Export-Crop {
  param(
    [System.Drawing.Bitmap]$SourceBitmap,
    [hashtable]$Slice,
    [string]$OutputPath
  )

  $sourceRect = [System.Drawing.Rectangle]::new(
    [int]$Slice.X,
    [int]$Slice.Y,
    [int]$Slice.W,
    [int]$Slice.H
  )
  $outputBitmap = [System.Drawing.Bitmap]::new(
    [int]$Slice.W,
    [int]$Slice.H,
    [System.Drawing.Imaging.PixelFormat]::Format32bppArgb
  )
  $graphics = [System.Drawing.Graphics]::FromImage($outputBitmap)
  try {
    $graphics.DrawImage(
      $SourceBitmap,
      [System.Drawing.Rectangle]::new(0, 0, [int]$Slice.W, [int]$Slice.H),
      $sourceRect,
      [System.Drawing.GraphicsUnit]::Pixel
    )
    Apply-SliceMask -Bitmap $outputBitmap -Slice $Slice
    $outputBitmap.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
  }
  finally {
    $graphics.Dispose()
    $outputBitmap.Dispose()
  }
}

foreach ($group in $slices) {
  $sourcePath = Join-Path $assetsRoot $group.Source
  if (-not (Test-Path $sourcePath)) {
    throw "Source image not found: $sourcePath"
  }

  $sourceBitmap = [System.Drawing.Bitmap]::FromFile((Resolve-Path $sourcePath))
  try {
    if ($sourceBitmap.Width -ne $referenceWidth -or $sourceBitmap.Height -ne $referenceHeight) {
      throw "Unexpected source size for $($group.Source): $($sourceBitmap.Width)x$($sourceBitmap.Height). Expected ${referenceWidth}x${referenceHeight}."
    }

    foreach ($slice in $group.Items) {
      $right = [int]$slice.X + [int]$slice.W
      $bottom = [int]$slice.Y + [int]$slice.H
      if ($right -gt $sourceBitmap.Width -or $bottom -gt $sourceBitmap.Height) {
        throw "Slice $($slice.Name) exceeds source bounds."
      }

      $outputPath = Join-Path $outputRoot "$($slice.Name).png"
      Export-Crop -SourceBitmap $sourceBitmap -Slice $slice -OutputPath $outputPath
      Write-Host "Wrote $outputPath"
    }
  }
  finally {
    $sourceBitmap.Dispose()
  }
}
