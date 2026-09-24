<#
.SYNOPSIS
    PortableHub 2.0 双版本自动化打包脚本 (含 WebView2 离线版 / 不含 WebView2 轻量版)

.DESCRIPTION
    一键编译并打包 PortableHub 2.0：
    - Light 模式：不内置 WebView2 Fixed Runtime，极简轻量 (~10-15MB)，依赖系统已装运行时或在线 Bootstrapper
    - Offline 模式：内置 WebView2 Fixed Runtime，纯内网离线解压即用
    - All 模式：同时输出两种版本的 Portable Zip 和 Setup 安装包

.EXAMPLE
    .\build_v2_packages.ps1 -Mode Light
    .\build_v2_packages.ps1 -Mode Offline -FixedRuntimePath "D:\Tools\Microsoft.WebView2.FixedVersionRuntime.120.0.2210.144.x64"
    .\build_v2_packages.ps1 -Mode All
#>

param(
    [ValidateSet("Light", "Offline", "All")]
    [string]$Mode = "Light",

    [string]$FixedRuntimePath = ""
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  PortableHub 2.0 打包系统 (当前模式: $Mode)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. 编译前端生产静态资源
Write-Host "`n[1/3] 编译前端生产环境资源 (Vite + Vue 3)..." -ForegroundColor Yellow
pnpm run build
if ($LASTEXITCODE -ne 0) {
    Write-Error "前端构建失败，请检查报错！"
    exit 1
}

$releaseDir = Join-Path $scriptDir "release"
if (-not (Test-Path $releaseDir)) {
    New-Item -ItemType Directory -Path $releaseDir | Out-Null
}

$tauriConfPath = Join-Path $scriptDir "src-tauri\tauri.conf.json"
$originalConfContent = Get-Content -Raw -Path $tauriConfPath -Encoding UTF8

function Build-LightVersion {
    Write-Host "`n----------------------------------------------------------" -ForegroundColor Green
    Write-Host "  正在构建：轻量版 (Light / 不含内置 WebView2)" -ForegroundColor Green
    Write-Host "----------------------------------------------------------" -ForegroundColor Green

    # 修改配置为 downloadBootstrapper
    $conf = Get-Content -Raw -Path $tauriConfPath -Encoding UTF8 | ConvertFrom-Json
    
    # 确保 bundle windows 配置
    if (-not $conf.bundle.windows) {
        $conf.bundle | Add-Member -MemberType NoteProperty -Name "windows" -Value (New-Object PSObject) -Force
    }
    $conf.bundle.windows.webviewInstallMode = "downloadBootstrapper"
    
    $conf | ConvertTo-Json -Depth 10 | Set-Content -Path $tauriConfPath -Encoding UTF8

    Write-Host "正在调用 cargo tauri build (Light)..." -ForegroundColor Gray
    pnpm tauri build

    # 归档便携版
    $binSource = Join-Path $scriptDir "src-tauri\target\release\portablehub-v2.exe"
    if (Test-Path $binSource) {
        $targetDir = Join-Path $releaseDir "PortableHub-v2-Light"
        if (Test-Path $targetDir) { Remove-Item -Recurse -Force $targetDir }
        New-Item -ItemType Directory -Path $targetDir | Out-Null
        New-Item -ItemType Directory -Path (Join-Path $targetDir "data") | Out-Null
        
        Copy-Item $binSource -Destination (Join-Path $targetDir "PortableHub.exe")
        
        # 压缩便携包
        $zipPath = Join-Path $releaseDir "PortableHub-v2-Portable-Light.zip"
        if (Test-Path $zipPath) { Remove-Item -Force $zipPath }
        Compress-Archive -Path "$targetDir\*" -DestinationPath $zipPath -CompressionLevel Optimal
        
        $sizeMB = (Get-Item $zipPath).Length / 1MB
        Write-Host ">> [完成] 便携包 (轻量版) 已生成: $zipPath (大小: $([math]::Round($sizeMB, 2)) MB)" -ForegroundColor Green
    }
}

function Build-OfflineVersion {
    Write-Host "`n----------------------------------------------------------" -ForegroundColor Magenta
    Write-Host "  正在构建：离线版 (Offline / 内置 WebView2 Fixed Runtime)" -ForegroundColor Magenta
    Write-Host "----------------------------------------------------------" -ForegroundColor Magenta

    $runtimeDir = $FixedRuntimePath
    if ([string]::IsNullOrWhiteSpace($runtimeDir)) {
        $localRuntime = Join-Path $scriptDir "webview2-fixed-runtime"
        if (Test-Path $localRuntime) {
            $runtimeDir = $localRuntime
        }
    }

    if ([string]::IsNullOrWhiteSpace($runtimeDir) -or -not (Test-Path $runtimeDir)) {
        Write-Warning "未找到 WebView2 Fixed Runtime 目录！"
        Write-Host "若要制作内置 WebView2 离线版本，请从微软官网下载 Microsoft.WebView2.FixedVersionRuntime 解压至：`n$scriptDir\webview2-fixed-runtime`n或使用参数 -FixedRuntimePath <路径>" -ForegroundColor Yellow
        Write-Host "跳过 Fixed Runtime 内嵌，构建独立离线便携包..." -ForegroundColor Yellow
    }

    # 修改配置为 fixedRuntime 或 skip
    $conf = Get-Content -Raw -Path $tauriConfPath -Encoding UTF8 | ConvertFrom-Json
    if (-not $conf.bundle.windows) {
        $conf.bundle | Add-Member -MemberType NoteProperty -Name "windows" -Value (New-Object PSObject) -Force
    }

    if ($runtimeDir -and (Test-Path $runtimeDir)) {
        $conf.bundle.windows.webviewInstallMode = @{
            type = "fixedRuntime"
            path = $runtimeDir
        }
    } else {
        $conf.bundle.windows.webviewInstallMode = "skip"
    }

    $conf | ConvertTo-Json -Depth 10 | Set-Content -Path $tauriConfPath -Encoding UTF8

    Write-Host "正在调用 cargo tauri build (Offline)..." -ForegroundColor Gray
    pnpm tauri build

    # 归档便携版
    $binSource = Join-Path $scriptDir "src-tauri\target\release\portablehub-v2.exe"
    if (Test-Path $binSource) {
        $targetDir = Join-Path $releaseDir "PortableHub-v2-Offline"
        if (Test-Path $targetDir) { Remove-Item -Recurse -Force $targetDir }
        New-Item -ItemType Directory -Path $targetDir | Out-Null
        New-Item -ItemType Directory -Path (Join-Path $targetDir "data") | Out-Null
        
        Copy-Item $binSource -Destination (Join-Path $targetDir "PortableHub.exe")
        
        if ($runtimeDir -and (Test-Path $runtimeDir)) {
            Copy-Item -Recurse $runtimeDir -Destination (Join-Path $targetDir "WebView2")
        }

        # 压缩便携包
        $zipPath = Join-Path $releaseDir "PortableHub-v2-Portable-Offline.zip"
        if (Test-Path $zipPath) { Remove-Item -Force $zipPath }
        Compress-Archive -Path "$targetDir\*" -DestinationPath $zipPath -CompressionLevel Optimal
        
        $sizeMB = (Get-Item $zipPath).Length / 1MB
        Write-Host ">> [完成] 便携包 (离线版) 已生成: $zipPath (大小: $([math]::Round($sizeMB, 2)) MB)" -ForegroundColor Green
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
    # 恢复原 tauri.conf.json 文件
    Set-Content -Path $tauriConfPath -Value $originalConfContent -Encoding UTF8
    Write-Host "`n配置已安全恢复。" -ForegroundColor Gray
}

Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host "  打包流程执行完毕！产物目录: $releaseDir" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
