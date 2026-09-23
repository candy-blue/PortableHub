<#
.SYNOPSIS
    Portable Hub 一键编译与安装包生成脚本

.DESCRIPTION
    编译 PortableHub.App 独立运行环境并调用 Inno Setup 编译器生成 PortableHub-Setup-x64.exe 安装包。

.PARAMETER Version
    指定生成的版本号，默认为 1.1.0

.PARAMETER Configuration
    编译配置，默认为 Release

.PARAMETER SkipPublish
    跳过 dotnet publish 步骤，直接使用现有的 build_standalone 进行打包
#>

param(
    [string]$Version = "1.2.0",
    [string]$Configuration = "Release",
    [switch]$SkipPublish = $false
)

$ErrorActionPreference = "Stop"
$RootDir = $PSScriptRoot

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  Portable Hub 安装版一键构建脚本" -ForegroundColor Cyan
Write-Host "  版本: v$Version | 配置: $Configuration" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. 编译发布独立目录 (build_standalone)
$StandaloneDir = Join-Path $RootDir "build_standalone"
if (-not $SkipPublish) {
    Write-Host "`n[1/3] 正在编译 .NET 8 独立运行环境 (build_standalone)..." -ForegroundColor Yellow
    dotnet publish (Join-Path $RootDir "PortableHub.App\PortableHub.App.csproj") `
        -c $Configuration `
        -r win-x64 `
        --self-contained true `
        -o $StandaloneDir

    # 规范化入口程序名 PortableHub.exe
    $AppExe = Join-Path $StandaloneDir "PortableHub.App.exe"
    $TargetExe = Join-Path $StandaloneDir "PortableHub.exe"
    if (Test-Path $AppExe) {
        Copy-Item -Path $AppExe -Destination $TargetExe -Force
    }

    $AppRuntimeConfig = Join-Path $StandaloneDir "PortableHub.App.runtimeconfig.json"
    $TargetRuntimeConfig = Join-Path $StandaloneDir "PortableHub.runtimeconfig.json"
    if (Test-Path $AppRuntimeConfig) {
        Copy-Item -Path $AppRuntimeConfig -Destination $TargetRuntimeConfig -Force
    }

    $AppDeps = Join-Path $StandaloneDir "PortableHub.App.deps.json"
    $TargetDeps = Join-Path $StandaloneDir "PortableHub.deps.json"
    if (Test-Path $AppDeps) {
        Copy-Item -Path $AppDeps -Destination $TargetDeps -Force
    }

    Write-Host "  √ 独立运行目录构建完成: $StandaloneDir" -ForegroundColor Green
} else {
    Write-Host "`n[1/3] 跳过编译，使用现有独立目录: $StandaloneDir" -ForegroundColor Gray
}

# 2. 确保 release 输出目录存在
$ReleaseDir = Join-Path $RootDir "release"
if (-not (Test-Path $ReleaseDir)) {
    New-Item -ItemType Directory -Path $ReleaseDir | Out-Null
}

# 3. 寻找 Inno Setup 编译器 (ISCC.exe)
Write-Host "`n[2/3] 检测 Inno Setup 6 编译器..." -ForegroundColor Yellow

$IsccPath = $null
$CommandCheck = Get-Command "iscc" -ErrorAction SilentlyContinue
if ($CommandCheck) {
    $IsccPath = $CommandCheck.Source
} else {
    $CandidatePaths = @(
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe",
        (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe")
    )
    foreach ($cand in $CandidatePaths) {
        if (Test-Path $cand) {
            $IsccPath = $cand
            break
        }
    }
}

if (-not $IsccPath) {
    Write-Host "  [提示] 未在系统 PATH 或常见安装路径中检测到 Inno Setup 6 编译器 (ISCC.exe)。" -ForegroundColor Yellow
    Write-Host "  请通过以下方式安装 Inno Setup 6：" -ForegroundColor Cyan
    Write-Host "    - 使用 Windows 包管理器: winget install JRSoftware.InnoSetup" -ForegroundColor White
    Write-Host "    - 或访问官网下载: https://jrsoftware.org/isdl.php" -ForegroundColor White
    Write-Host "`n  注: 独立运行环境已输出至 build_standalone\，安装 Inno Setup 后即可一键打包。" -ForegroundColor Gray
    exit 0
}

Write-Host "  √ 找到 Inno Setup 编译器: $IsccPath" -ForegroundColor Green

# 4. 执行 Inno Setup 打包
Write-Host "`n[3/3] 正在打包 PortableHub-Setup-x64.exe..." -ForegroundColor Yellow
$IssFile = Join-Path $RootDir "installer\PortableHub.iss"

& $IsccPath "/DMyAppVersion=$Version" $IssFile

$InstallerOutput = Join-Path $ReleaseDir "PortableHub-Setup-x64.exe"
if (Test-Path $InstallerOutput) {
    $fileItem = Get-Item $InstallerOutput
    $sizeMb = [Math]::Round($fileItem.Length / 1MB, 2)
    Write-Host "`n==========================================================" -ForegroundColor Green
    Write-Host "  √ 安装包构建成功!" -ForegroundColor Green
    Write-Host "  文件: $InstallerOutput" -ForegroundColor White
    Write-Host "  大小: $sizeMb MB" -ForegroundColor White
    Write-Host "==========================================================" -ForegroundColor Green
} else {
    Write-Host "`n[错误] 未在 release 目录中找到生成的安装包文件。" -ForegroundColor Red
    exit 1
}
