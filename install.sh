#!/bin/bash
# LPU+ Agent Global Installer
# Run with: curl -sSL https://raw.githubusercontent.com/lpu-software/LPUplus/main/install.sh | bash

set -e

echo "======================================"
echo "  LPU+ Agent Zero-Install Setup       "
echo "======================================"

# 1. Install .NET 8 SDK if missing
if ! command -v dotnet &> /dev/null || ! dotnet --list-sdks | grep -q "^8\.0"; then
    echo "[1/3] Installing .NET 8.0 SDK (this may take a minute)..."
    curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0
    export PATH="$HOME/.dotnet:$PATH"
else
    echo "[1/3] .NET 8.0 SDK already installed."
fi

# 2. Clone/Update Repo
INSTALL_DIR="$HOME/.lpuplus"
if [ -d "$INSTALL_DIR" ] && [ -d "$INSTALL_DIR/.git" ]; then
    echo "[2/3] Updating existing LPU+ Agent..."
    cd "$INSTALL_DIR"
    git pull origin main --quiet
else
    echo "[2/3] Downloading LPU+ Agent..."
    rm -rf "$INSTALL_DIR"
    git clone --quiet https://github.com/lpu-software/LPUplus.git "$INSTALL_DIR"
    cd "$INSTALL_DIR"
fi

# 3. Start the Agent
echo "[3/3] Starting LPU+ Agent..."
chmod +x ./run-agent.sh
./run-agent.sh start

echo "======================================"
echo "✅ Installation Complete!"
echo "   Agent is running in the background."
echo "   To manage it, navigate to: $INSTALL_DIR"
echo "   Commands: ./run-agent.sh [start|stop|log|status]"
echo "======================================"
