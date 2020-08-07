#!/bin/bash
# pullusers.sh
# Backup users to git, override everything
cd Resources/Accounts/
git commit -a -m "Backup Accounts before Pulling"
git pull -s recursive -X theirs
git push

SERVICE=${1:-"MedoiBotService"}
sudo systemctl restart "$SERVICE"
