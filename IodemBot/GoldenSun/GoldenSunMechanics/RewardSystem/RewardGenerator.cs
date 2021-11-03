using System;
using System.Collections.Generic;
using System.Linq;

namespace IodemBot.Modules.GoldenSunMechanics.RewardSystem
{
    public class RewardGenerator<T>
    {
        private readonly List<RewardStruct> _rewards = new();

        public RewardGenerator(IEnumerable<T> rewards, IEnumerable<int> weights)
        {
            if (rewards.Count() != weights.Count())
                throw new ArgumentException("Length of rewards and weights must match up.");
            for (var i = 0; i < rewards.Count(); i++) AddReward(rewards.ElementAt(i), weights.ElementAt(i));
        }

        public void AddReward(T reward, int weight)
        {
            _rewards.Add(new RewardStruct(reward, weight));
        }

        public T GenerateReward()
        {
            if (_rewards.Count == 0) throw new InvalidOperationException("Sequence contains no elements");

            var weight = _rewards.Sum(d => d.Weight);

            var roll = Global.RandomNumber(0, weight);
            var sortedRewards = _rewards.OrderByDescending(d => d.Weight).ToList();
            var reward = sortedRewards.SkipWhile(r => (roll -= r.Weight) >= 0).FirstOrDefault().Reward;

            return reward;
        }

        private struct RewardStruct
        {
            internal readonly T Reward;
            internal readonly int Weight;

            public RewardStruct(T reward, int weight) : this()
            {
                Reward = reward;
                Weight = weight;
            }
        }
    }
}