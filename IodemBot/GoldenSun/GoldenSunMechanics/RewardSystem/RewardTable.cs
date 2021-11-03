using System.Collections.Generic;
using System.Linq;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class RewardTable : List<Rewardable>
    {
        public int Weight
        {
            get { return this.Sum(f => f.Weight); }
        }

        public Rewardable RollReward()
        {
            if (Count == 0) return new DefaultReward();

            var roll = Global.RandomNumber(0, Weight);
            //Console.WriteLine($"{roll + 1}/{Weight}");
            var sortedRewards = this.OrderByDescending(d => d.Weight).ToList();
            var reward = sortedRewards.SkipWhile(r => (roll -= r.Weight) >= 0).FirstOrDefault() ?? new DefaultReward();

            return reward;
        }
    }

    public class RewardTables : List<RewardTable>
    {
        public List<Rewardable> GetRewards()
        {
            if (Count == 0) Add(new RewardTable());

            return this.Select(d => d.RollReward()).ToList();
        }
    }
}