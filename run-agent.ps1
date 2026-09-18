param (
    [string]$Command = "start"
)

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$Project = Join-Path $ScriptDir "src\LPUPlus.Agent\LPUPlus.Agent.csproj"
$LogFile = Join-Path $ScriptDir "agent.log"
$PidFile = Join-Path $ScriptDir "agent.pid"

if ($Command -eq "start") {
    if (Test-Path $PidFile) {
        $existingPid = Get-Content $PidFile
        if (Get-Process -Id $existingPid -ErrorAction SilentlyContinue) {
            Write-Host "Agent is already running (PID: $existingPid)"
            Write-Host "Use: .\run-agent.ps1 stop"
            exit 1
        }
    }

    Write-Host "Starting LPU+ Agent in background..."
    $process = Start-Process -FilePath "dotnet" -ArgumentList "run --project `"$Project`" -- start" -RedirectStandardOutput $LogFile -RedirectStandardError $LogFile -PassThru -WindowStyle Hidden
    $process.Id | Out-File -FilePath $PidFile -Encoding ASCII

    for ($i = 0; $i -lt 10; $i++) {
        Start-Sleep -Seconds 1
        if (Test-Path $LogFile) {
            $pairingCode = Select-String -Path $LogFile -Pattern "[A-Z0-9]{4}-[A-Z0-9]{4}" | Select-Object -ExpandProperty Matches | Select-Object -ExpandProperty Value | Select-Object -Last 1
            if ($pairingCode) {
                break
            }
        }
    }

    Write-Host "✅ Agent started (PID: $($process.Id))"
    Write-Host "   Log: $LogFile"
    if ($pairingCode) {
        Write-Host "   🎉 Pairing Code: $pairingCode" -ForegroundColor Cyan
    } else {
        Write-Host "   (Still starting up... check log to see the code when ready)"
    }
    Write-Host "   To stop: .\run-agent.ps1 stop"
}
elseif ($Command -eq "stop") {
    if (Test-Path $PidFile) {
        $pidToKill = Get-Content $PidFile
        $proc = Get-Process -Id $pidToKill -ErrorAction SilentlyContinue
        if ($proc) {
            Stop-Process -Id $pidToKill -Force
            Remove-Item $PidFile -Force
            Write-Host "✅ Agent stopped (PID: $pidToKill)"
        } else {
            Remove-Item $PidFile -Force
            Write-Host "Agent was not running."
        }
    } else {
        Write-Host "No agent PID file found."
    }

    Write-Host "🧹 Cleaning up downloaded files..."
    Set-Location $env:USERPROFILE
    Remove-Item (Join-Path $env:USERPROFILE ".lpuplus") -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "✅ All files deleted."
    Write-Host "⚠️  Note: If your terminal was inside the .lpuplus folder, type 'cd ~' to return to your home directory."
}
elseif ($Command -eq "status") {
    if (Test-Path $PidFile) {
        $pidToCheck = Get-Content $PidFile
        if (Get-Process -Id $pidToCheck -ErrorAction SilentlyContinue) {
            Write-Host "✅ Agent is running (PID: $pidToCheck)"
        } else {
            Write-Host "Agent is not running."
        }
    } else {
        Write-Host "Agent is not running."
    }
}
else {
    Write-Host "Usage: .\run-agent.ps1 [start|stop|status]"
}
