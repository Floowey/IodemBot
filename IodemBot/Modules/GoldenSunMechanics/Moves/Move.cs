using IodemBot.Modules.ColossoBattles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IodemBot.Modules.GoldenSunMechanics 
{
    public enum Target { self, ownSingle, ownAll, otherSingle, otherRange, otherAll}
    public abstract class Move : ICloneable
    {
        public string name;
        public string emote;
        public Target targetType;
        public int targetNr;
        public uint range;
        

        public abstract List<string> Use(ColossoFighter User);

        public Move(string name, string emote, Target targetType, uint range)
        {
            this.name = name;
            this.emote = emote;
            this.targetType = targetType;
            this.range = range;
        }

        public List<ColossoFighter> getTarget(ColossoFighter user)
        {
            List<ColossoFighter> targets = new List<ColossoFighter>();
            switch (targetType)
            {
                case Target.self:
                    targets.Add(user);
                    break;
                case Target.ownAll:
                    targets.AddRange(user.battle.getTeam(user.party));
                    break;
                case Target.ownSingle:
                    targets.Add(user.battle.getTeam(user.party)[targetNr]);
                    break;
                case Target.otherAll:
                    targets.AddRange(user.battle.getTeam(user.enemies));
                    break;
                case Target.otherSingle:
                    targets.Add(user.battle.getTeam(user.enemies)[targetNr]);
                    break;
                case Target.otherRange:
                    var targetTeam = user.battle.getTeam(user.enemies);
                    for (int i = -(int)range + 1; i <= range - 1; i++)
                    {
                        if (targetNr + i >= 0 && targetNr + i < targetTeam.Count())
                        {
                            targets.Add(targetTeam[targetNr + i]);
                        }
                    }
                    break;
            }
            return targets;
        }

        public abstract object Clone();
    }
}
