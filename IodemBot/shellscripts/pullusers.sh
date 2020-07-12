#!/bin/bash
# pullusers.sh
# Backup users to git, override everything
cd ~/bot/Resources/Accounts/
git commit -a -m "Backup Accounts"
git pull -s recursive -X theirs
git push
sudo systemctl restart IodemBotService