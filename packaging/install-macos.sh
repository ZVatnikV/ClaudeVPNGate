#!/bin/bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")" && pwd)"
osascript -e "do shell script \"rm -rf '/Applications/Claude VPN Gate.app' && cp -R '$ROOT/Claude VPN Gate.app' /Applications && chmod +x '/Applications/Claude VPN Gate.app/Contents/MacOS/ClaudeVPNGate'\" with administrator privileges"
echo "Installed in /Applications. The app will ask for an administrator password."
