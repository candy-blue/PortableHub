param(
    [ValidateSet("Light", "Offline", "All")]
    [string]$Mode = "Light",

    [string]$FixedRuntimePath = "",
    [switch]$SkipCargoBuild
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  PortableHub 2.0 Packaging Script (Mode: $Mode)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Build frontend if needed
if (-not $SkipCargoBuild) {
    Write-Host "`n[1/3] Building frontend assets (Vite + Vue 3)..." -ForegroundColor Yellow
    pnpm run build
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Frontend build failed!"
        exit 1
    }
}

$releaseDir = Join-Path $scriptDir "release"
if (-not (Test-Path $releaseDir)) {
    New-Item -ItemType Directory -Path $releaseDir | Out-Null
}

$tauriConfPath = Join-Path $scriptDir "src-tauri\tauri.conf.json"
$originalConfContent = Get-Content -Raw -Path $tauriConfPath -Encoding UTF8

function Get-ReleaseExePath {
    $candidates = @(
        (Join-Path $scriptDir "src-tauri\target\release\portablehub-v2.exe"),
        (Join-Path $scriptDir "src-tauri\target\release\PortableHub.exe")
    )
    foreach ($cand in $candidates) {
        if (Test-Path $cand) { return $cand }
    }
    return $null
}

function Build-LightVersion {
    Write-Host "`n----------------------------------------------------------" -ForegroundColor Green
    Write-Host "  Building: Light Version (No embedded WebView2)" -ForegroundColor Green
    Write-Host "----------------------------------------------------------" -ForegroundColor Green

    if (-not $SkipCargoBuild) {
        Write-Host "Building Tauri release binary..." -ForegroundColor Gray
        pnpm tauri build --no-bundle
    }

    $binSource = Get-ReleaseExePath
    if ($binSource) {
        $targetDir = Join-Path $releaseDir "PortableHub-v2-Light"
        if (Test-Path $targetDir) { Remove-Item -Recurse -Force $targetDir }
        New-Item -ItemType Directory -Path $targetDir | Out-Null
        New-Item -ItemType Directory -Path (Join-Path $targetDir "data") | Out-Null
        
        Copy-Item $binSource -Destination (Join-Path $targetDir "PortableHub.exe")
        
        # Zip portable package
        $zipPath = Join-Path $releaseDir "PortableHub-v2-Portable-Light.zip"
        if (Test-Path $zipPath) { Remove-Item -Force $zipPath }
        Compress-Archive -Path "$targetDir\*" -DestinationPath $zipPath -CompressionLevel Optimal
        
        $sizeMB = (Get-Item $zipPath).Length / 1MB
        Write-Host ">> [SUCCESS] Light Portable Package: $zipPath ($([math]::Round($sizeMB, 2)) MB)" -ForegroundColor Green
    } else {
        Write-Error "Release executable not found in src-tauri/target/release!"
    }
}

function Build-OfflineVersion {
    Write-Host "`n----------------------------------------------------------" -ForegroundColor Magenta
    Write-Host "  Building: Offline Version (Embedded WebView2 Runtime)" -ForegroundColor Magenta
    Write-Host "----------------------------------------------------------" -ForegroundColor Magenta

    $runtimeDir = $FixedRuntimePath
    if ([string]::IsNullOrWhiteSpace($runtimeDir)) {
        $localRuntime = Join-Path $scriptDir "webview2-fixed-runtime"
        if (Test-Path $localRuntime) {
            $runtimeDir = $localRuntime
        }
    }

    if ([string]::IsNullOrWhiteSpace($runtimeDir) -or -not (Test-Path $runtimeDir)) {
        Write-Warning "WebView2 Fixed Runtime directory not found at $runtimeDir."
        Write-Host "To bundle WebView2 offline, download Microsoft.WebView2.FixedVersionRuntime and place it into:" -ForegroundColor Yellow
        Write-Host "  $scriptDir\webview2-fixed-runtime" -ForegroundColor Yellow
        Write-Host "or provide -FixedRuntimePath <Path>" -ForegroundColor Yellow
        Write-Host "Packaging offline portable structure without embedded runtime folder for now..." -ForegroundColor Yellow
    }

    if (-not $SkipCargoBuild) {
        Write-Host "Building Tauri release binary..." -ForegroundColor Gray
        pnpm tauri build --no-bundle
    }

    $binSource = Get-ReleaseExePath
    if ($binSource) {
        $targetDir = Join-Path $releaseDir "PortableHub-v2-Offline"
        if (Test-Path $targetDir) { Remove-Item -Recurse -Force $targetDir }
        New-Item -ItemType Directory -Path $targetDir | Out-Null
        New-Item -ItemType Directory -Path (Join-Path $targetDir "data") | Out-Null
        
        Copy-Item $binSource -Destination (Join-Path $targetDir "PortableHub.exe")
        
        if ($runtimeDir -and (Test-Path $runtimeDir)) {
            Write-Host "Copying WebView2 Fixed Runtime..." -ForegroundColor Gray
            Copy-Item -Recurse $runtimeDir -Destination (Join-Path $targetDir "WebView2")
        }

        # Zip portable package
        $zipPath = Join-Path $releaseDir "PortableHub-v2-Portable-Offline.zip"
        if (Test-Path $zipPath) { Remove-Item -Force $zipPath }
        Compress-Archive -Path "$targetDir\*" -DestinationPath $zipPath -CompressionLevel Optimal
        
        $sizeMB = (Get-Item $zipPath).Length / 1MB
        Write-Host ">> [SUCCESS] Offline Portable Package: $zipPath ($([math]::Round($sizeMB, 2)) MB)" -ForegroundColor Green
    } else {
        Write-Error "Release executable not found in src-tauri/target/release!"
    }
}

try {
    if ($Mode -eq "Light" -or $Mode -eq "All") {
        Build-LightVersion
    }
    if ($Mode -eq "Offline" -or $Mode -eq "All") {
        Build-OfflineVersion
    }
}
finally {
    Set-Content -Path $tauriConfPath -Value $originalConfContent -Encoding UTF8
    Write-Host "`nTauri configuration verified." -ForegroundColor Gray
}

Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host "  Build completed successfully! Output: $releaseDir" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
