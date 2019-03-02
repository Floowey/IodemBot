using Discord.WebSocket;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;
using IodemBot.Modules.GoldenSunMechanics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IodemBot.Modules.ColossoBattles
{
    public class PlayerFighter : ColossoFighter
    {
        public UserAccount avatar;

        private static Stats baseStats = new Stats(30, 20, 20, 6, 8); //30, 20, 11, 6, 8

        public PlayerFighter(SocketGuildUser user) : base(user.DisplayName(), user.GetAvatarUrl(),
            ModifyStats(user),
            AdeptClassSeriesManager.getElStats(UserAccounts.GetAccount(user)),
            AdeptClassSeriesManager.getMoveset(UserAccounts.GetAccount(user)))
        {
            avatar = UserAccounts.GetAccount(user);
        }

        private static Stats ModifyStats(SocketGuildUser user)
        {
            var avatar = UserAccounts.GetAccount(user);
            var adept = AdeptClassSeriesManager.getClass(avatar);
            var multipliers = adept.statMultipliers;
            var level = avatar.LevelNumber;
           
            var actualStats = new Stats(
                (uint)((baseStats.maxHP + baseStats.maxHP * 0.25 * level / 1.4) * multipliers.maxHP / 100),
                (uint)((baseStats.maxPP + baseStats.maxPP * 0.115 * level / 1.2) * multipliers.maxPP / 100),
                (uint)((baseStats.Atk + baseStats.Atk * 0.3 * level / 1.5) *  multipliers.Atk / 100),
                (uint)((baseStats.Def + baseStats.Def * 0.3 * level / 1.5) *  multipliers.Def / 100 ),
                (uint)((baseStats.Spd + baseStats.Spd * 0.5 * level / 1.5) *  multipliers.Spd / 100 ));
            return actualStats;
        }

        public override List<string> EndTurn()
        {
            selected = null;
            hasSelected = false;
            return base.EndTurn();
        }

    }
}
