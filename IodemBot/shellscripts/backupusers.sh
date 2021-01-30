#!/bin/bash
# backupusers.sh
# Backup users to git, override everything
cd Resources/Accounts/
git add -A
git commit -a -m "Backup Accounts"
git pull -s recursive -X ours
git push
