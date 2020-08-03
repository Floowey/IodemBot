#!/bin/bash
# selfupdate.sh
# Self Update, this should be able to be called from within the bot itself
echo "Start Updating"
cd ../repo/IodemBot # Folder of Repository
git pull
echo "done pulling"
dotnet publish -o ../../bin # Folder of compiled program
echo "done publishing"
sudo systemctl restart IodemBotService # Background service linked to /home/pi/bot/IodemBot.dll
echo "process started"
