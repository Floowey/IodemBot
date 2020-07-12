#!/bin/bash
# selfupdate.sh
# Self Update, this should be able to be called from within the bot itself
killall IodemBot
cd ~/IodemBot/IodemBot/
git pull
dotnet publish -o /home/pi/bot/
cd /home/pi/bot
dotnet IodemBot.dll