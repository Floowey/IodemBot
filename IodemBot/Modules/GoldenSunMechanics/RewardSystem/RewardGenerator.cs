using System;
using System.Collections.Generic;
using System.Linq;

namespace IodemBot.Modules.GoldenSunMechanics.RewardSystem
{
    public class RewardGenerator<T>
    {
        readonly List<Reward> Rewards = new List<Reward>();
        public RewardGenerator(IEnumerable<T> rewards, IEnumerable<int> weights)
        {
            if (rewards.Count() != weights.Count())
            {
                throw new ArgumentException("Length of rewards and weights must match up.");
            }
            for (int i = 0; i < rewards.Count(); i++)
            {
                AddReward(rewards.ElementAt(i), weights.ElementAt(i));
            }
        }

        public void AddReward(T reward, int weight)
        {
            Rewards.Add(new Reward(reward, weight));
        }

        public T GenerateReward()
        {
            if (Rewards.Count == 0)
            {
                throw new InvalidOperationException("Sequence contains no elements");
            }

            var weight = Rewards.Sum(d => d.weight);

            int roll = Global.RandomNumber(0, weight);
            var sortedRewards = Rewards.OrderByDescending(d => d.weight).ToList();
            var reward = sortedRewards.SkipWhile(r => (roll -= r.weight) >= 0).FirstOrDefault().reward;

            return reward;
        }

        private struct Reward
        {
            internal T reward;
            internal int weight;

            public Reward(T reward, int weight) : this()
            {
                this.reward = reward;
                this.weight = weight;
            }
        }
    }
}
