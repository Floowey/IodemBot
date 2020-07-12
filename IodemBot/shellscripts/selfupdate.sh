#!/bin/bash
# selfupdate.sh
# Self Update, this should be able to be called from within the bot itself
sudo systemctl stop IodemBotService
cd ~/IodemBot/IodemBot/
git pull
dotnet publish -o /home/pi/bot/
sudo systemctl start IodemBotService