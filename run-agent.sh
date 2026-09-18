#!/bin/bash
# LPU+ Agent Background Launcher
# Starts the Agent in the background so you can close the terminal.

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT="$SCRIPT_DIR/src/LPUPlus.Agent/LPUPlus.Agent.csproj"
LOG_FILE="$SCRIPT_DIR/agent.log"
PID_FILE="$SCRIPT_DIR/agent.pid"

case "${1:-start}" in
  start)
    if [ -f "$PID_FILE" ] && kill -0 "$(cat "$PID_FILE")" 2>/dev/null; then
      echo "Agent is already running (PID: $(cat "$PID_FILE"))"
      echo "Use: ./run-agent.sh stop"
      exit 1
    fi

    echo "Starting LPU+ Agent in background..."
    nohup dotnet run --project "$PROJECT" -- start > "$LOG_FILE" 2>&1 &
    echo $! > "$PID_FILE"
    
    # Wait up to 10 seconds for startup and pairing code
    for i in {1..10}; do
      sleep 1
      PAIRING_CODE=$(grep -A 1 "Pairing Code:" "$LOG_FILE" | tail -1 | grep -o '[A-Z0-9]\{4\}-[A-Z0-9]\{4\}' || true)
      if [ -n "$PAIRING_CODE" ]; then
        break
      fi
    done
    
    if kill -0 "$(cat "$PID_FILE")" 2>/dev/null; then
      echo "✅ Agent started (PID: $(cat "$PID_FILE"))"
      echo "   Log: $LOG_FILE"
      echo ""
      if [ -n "$PAIRING_CODE" ]; then
        echo "   🎉 Pairing Code: $PAIRING_CODE"
      else
        echo "   (Still starting up... run './run-agent.sh log' to see the code when ready)"
      fi
      echo ""
      echo "   To stop: ./run-agent.sh stop"
      echo "   To view log: tail -f $LOG_FILE"
    else
      echo "❌ Agent failed to start. Check $LOG_FILE"
      exit 1
    fi
    ;;

  stop)
    if [ -f "$PID_FILE" ]; then
      PID=$(cat "$PID_FILE")
      if kill -0 "$PID" 2>/dev/null; then
        kill "$PID"
        rm -f "$PID_FILE"
        echo "✅ Agent stopped (PID: $PID)"
      else
        rm -f "$PID_FILE"
        echo "Agent was not running."
      fi
    else
      echo "No agent PID file found."
    fi

    echo "🧹 Cleaning up downloaded files..."
    cd ~ || exit
    rm -rf ~/.lpuplus
    echo "✅ All files deleted."
    echo "⚠️  Note: If your terminal was inside the .lpuplus folder, type 'cd ~' to return to your home directory."
    ;;

  log)
    if [ -f "$LOG_FILE" ]; then
      tail -f "$LOG_FILE"
    else
      echo "No log file found."
    fi
    ;;

  status)
    if [ -f "$PID_FILE" ] && kill -0 "$(cat "$PID_FILE")" 2>/dev/null; then
      echo "✅ Agent is running (PID: $(cat "$PID_FILE"))"
    else
      echo "Agent is not running."
    fi
    ;;

  *)
    echo "Usage: ./run-agent.sh [start|stop|log|status]"
    ;;
esac
