#!/bin/bash
# selfupdate.sh
# Self Update, this should be able to be called from within the bot itself
echo "Start Updating"
cd ~/IodemBot/IodemBot/
git pull
echo "done pulling"
dotnet publish -o /home/pi/bot/
echo "done publishing"
sudo systemctl restart IodemBotService
echo "process started"
