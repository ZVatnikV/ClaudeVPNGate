#!/bin/sh
set -eu
ROOT="$(CDPATH= cd -- "$(dirname "$0")" && pwd)"
sudo install -d /opt/claude-vpn-gate
sudo cp -a "$ROOT/app/." /opt/claude-vpn-gate/
sudo chmod +x /opt/claude-vpn-gate/ClaudeVPNGate
sudo tee /usr/share/applications/claude-vpn-gate.desktop >/dev/null <<'EOF'
[Desktop Entry]
Type=Application
Name=Claude VPN Gate
Exec=pkexec /opt/claude-vpn-gate/ClaudeVPNGate
Icon=claude-vpn-gate
Categories=Utility;Security;
EOF
sudo ln -sfn /opt/claude-vpn-gate/ClaudeVPNGate /usr/local/bin/claude-vpn-gate
echo "Installed. Start it with: pkexec /opt/claude-vpn-gate/ClaudeVPNGate"
