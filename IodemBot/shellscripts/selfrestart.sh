#!/bin/bash
# selfupdate.sh
# Self Update, this should be able to be called from within the bot itself
echo "Starting Restart"

SERVICE=${1:-"MedoiBotService"}
sudo systemctl restart "$SERVICE" # Background service linked to /home/pi/bot/IodemBot.dll
