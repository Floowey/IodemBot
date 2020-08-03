#!/bin/bash
# backupusers.sh
# Backup users to git, override everything
cd ~/bot/Resources/Accounts/
git commit -a -m "Backup Accounts"
git pull -s recursive -X ours
git push
