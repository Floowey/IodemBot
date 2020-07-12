#!/bin/bash
# selfupdate.sh
# Self Update, this should be able to be called from within the bot itself
<<<<<<< HEAD
sudo systemctl stop IodemBotService
cd ~/IodemBot/IodemBot/
git pull
dotnet publish -o /home/pi/bot/
sudo systemctl start IodemBotService
=======
sudo systemctl stop IodemBotService.service
cd ~/IodemBot/IodemBot/
git pull
dotnet publish -o /home/pi/bot/
cd /home/pi/bot
sudo systemctl restart IodemBotService.service
>>>>>>> 12a6c440b347788ec066d7bf9ce3bfdc961677e8
