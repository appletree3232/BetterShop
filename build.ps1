# BetterShop 构建与打包脚本
# 使用方法: 在项目根目录运行 .\build.ps1

param(
    [switch]$PackOnly,      # 仅打包，不编译
    [switch]$Release        # Release 模式编译
)

$ErrorActionPreference = "Stop"

$ProjectDir = "$PSScriptRoot\BetterShop"
$ThunderstoreDir = "$PSScriptRoot\thunderstore"
$BuildDir = "$ThunderstoreDir\build"
$OutputZip = "$PSScriptRoot\BetterShop.zip"

# ── 编译 ──
if (-not $PackOnly) {
    Write-Host "=== 编译 BetterShop ===" -ForegroundColor Cyan

    $config = if ($Release) { "Release" } else { "Debug" }
    dotnet build "$ProjectDir\BetterShop.csproj" --configuration $config

    if ($LASTEXITCODE -ne 0) {
        Write-Host "编译失败！" -ForegroundColor Red
        exit 1
    }

    Write-Host "编译成功！" -ForegroundColor Green
}

# ── 打包 Thunderstore ──
Write-Host ""
Write-Host "=== 打包 Thunderstore ==="  -ForegroundColor Cyan

# 清理并创建构建目录
if (Test-Path $BuildDir) {
    Remove-Item $BuildDir -Recurse -Force
}
New-Item -ItemType Directory -Path $BuildDir | Out-Null
New-Item -ItemType Directory -Path "$BuildDir\plugins\BetterShop" | Out-Null

# 复制文件
Copy-Item "$ThunderstoreDir\manifest.json" "$BuildDir\" -Force
Copy-Item "$ThunderstoreDir\README.md" "$BuildDir\" -Force
Copy-Item "$ThunderstoreDir\icon.png" "$BuildDir\" -Force

# 复制 DLL
$dllPath = "$ProjectDir\bin\$config\BetterShop.dll"
if (-not (Test-Path $dllPath)) {
    # 尝试不带 config 子目录
    $dllPath = "$ProjectDir\bin\BetterShop.dll"
}

if (Test-Path $dllPath) {
    Copy-Item $dllPath "$BuildDir\plugins\BetterShop\" -Force
    Write-Host "已复制 BetterShop.dll" -ForegroundColor Green
} else {
    Write-Host "警告: 找不到 BetterShop.dll，请先编译项目" -ForegroundColor Yellow
}

# 创建 ZIP
if (Test-Path $OutputZip) {
    Remove-Item $OutputZip -Force
}
Compress-Archive -Path "$BuildDir\*" -DestinationPath $OutputZip -Force

Write-Host ""
Write-Host "=== 打包完成 ===" -ForegroundColor Green
Write-Host "输出文件: $OutputZip"
Write-Host ""
Write-Host "发布步骤:" -ForegroundColor Yellow
Write-Host "1. 前往 https://thunderstore.io/c/enter-the-gungeon/"
Write-Host "2. 登录账号并上传 BetterShop.zip"
Write-Host "3. 你的朋友可以通过 Thunderstore Mod Manager 搜索安装"
