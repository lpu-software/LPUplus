Write-Host "======================================"
Write-Host "  LPU+ Agent Zero-Install Setup       "
Write-Host "======================================"

# 1. Check .NET 8 SDK
$dotnetInstalled = $false
if (Get-Command dotnet -ErrorAction SilentlyContinue) {
    $sdks = dotnet --list-sdks
    if ($sdks -match "8\.0") {
        $dotnetInstalled = $true
    }
}

if (-not $dotnetInstalled) {
    Write-Host "[1/3] Installing .NET 8.0 SDK (this may take a minute)..."
    Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile "dotnet-install.ps1"
    .\dotnet-install.ps1 -Channel 8.0
    Remove-Item "dotnet-install.ps1"
    $env:PATH = "$env:LOCALAPPDATA\Microsoft\dotnet;$env:PATH"
} else {
    Write-Host "[1/3] .NET 8.0 SDK already installed."
}

# 1.5 Check FFmpeg
if (-not (Get-Command ffmpeg -ErrorAction SilentlyContinue)) {
    Write-Host "[1.5/3] Installing FFmpeg (required for screen capture)..."
    $FfmpegDir = Join-Path $env:USERPROFILE ".lpuplus\ffmpeg"
    if (-not (Test-Path $FfmpegDir)) {
        New-Item -ItemType Directory -Force -Path $FfmpegDir | Out-Null
        $ZipPath = Join-Path $env:TEMP "ffmpeg.zip"
        Invoke-WebRequest -Uri "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip" -OutFile $ZipPath
        Expand-Archive -Path $ZipPath -DestinationPath $FfmpegDir -Force
        Remove-Item $ZipPath -Force
    }
    $env:PATH = "$FfmpegDir\ffmpeg-master-latest-win64-gpl\bin;$env:PATH"
}

# 2. Clone/Update Repo
$InstallDir = Join-Path $env:USERPROFILE ".lpuplus"
if ((Test-Path $InstallDir) -and (Test-Path (Join-Path $InstallDir ".git"))) {
    Write-Host "[2/3] Updating existing LPU+ Agent..."
    Set-Location $InstallDir
    if (Get-Command git -ErrorAction SilentlyContinue) {
        git pull origin main --quiet
    }
} else {
    Write-Host "[2/3] Downloading LPU+ Agent..."
    if (Test-Path $InstallDir) {
        Remove-Item $InstallDir -Recurse -Force -ErrorAction SilentlyContinue
    }
    if (Get-Command git -ErrorAction SilentlyContinue) {
        git clone --quiet https://github.com/lpu-software/LPUplus.git $InstallDir
        Set-Location $InstallDir
    } else {
        $ZipPath = Join-Path $env:TEMP "lpuplus.zip"
        Invoke-WebRequest -Uri "https://github.com/lpu-software/LPUplus/archive/refs/heads/main.zip" -OutFile $ZipPath
        Expand-Archive -Path $ZipPath -DestinationPath $env:TEMP -Force
        Rename-Item -Path (Join-Path $env:TEMP "LPUplus-main") -NewName ".lpuplus"
        Move-Item -Path (Join-Path $env:TEMP ".lpuplus") -Destination $env:USERPROFILE -Force
        Remove-Item $ZipPath -Force
        Set-Location $InstallDir
    }
}

# 3. Start Agent
Write-Host "[3/3] Starting LPU+ Agent..."
.\run-agent.ps1 start

Write-Host "======================================"
Write-Host "✅ Installation Complete!"
Write-Host "   Agent is running in the background."
Write-Host "   To manage it, navigate to: $InstallDir"
Write-Host "   Commands: .\run-agent.ps1 stop"
Write-Host "======================================"
