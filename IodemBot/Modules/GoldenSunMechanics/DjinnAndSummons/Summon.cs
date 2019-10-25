using IodemBot.Modules.ColossoBattles;
using System.Collections.Generic;
using System.Linq;

namespace IodemBot.Modules.GoldenSunMechanics.DjinnAndSummons
{
    public class Summon : Move
    {
        public override string Name { get => Move.Name; set => Move.Name = value; }
        public override string Emote { get => Sprite; set => Move.Emote = value; }
        public override Target TargetType { get => Move.TargetType; set => Move.TargetType = value; }
        public override List<Effect> Effects { get => Move.Effects; set => Move.Effects = value; }
        public override int TargetNr { get => Move.TargetNr; set => Move.TargetNr = value; }
        public override uint Range { get => Move.Range; set => Move.Range = value; }
        public override bool HasPriority { get => Move.HasPriority; set => Move.HasPriority = value; }
        private string Sprite { get; set; }
        private int[] DjinnNeeded { get; set; } = { 0, 0, 0, 0 };
        private Move Move { get; set; }

        public override object Clone()
        {
            throw new System.NotImplementedException();
        }

        public override void InternalChooseBestTarget(ColossoFighter User)
        {
            Move.ChooseBestTarget(User);
        }

        public override bool InternalValidSelection(ColossoFighter User)
        {
            var PartyDjinn = User.GetTeam().SelectMany(u => u.Moves.OfType<Djinn>()).Distinct();
            var ReadyDjinn = PartyDjinn.Where(d => d.State == DjinnState.Set);
            return ReadyDjinn.Count(d => d.Element == Element.Venus) >= DjinnNeeded[1] &&
                ReadyDjinn.Count(d => d.Element == Element.Mars) >= DjinnNeeded[2] &&
                ReadyDjinn.Count(d => d.Element == Element.Jupiter) >= DjinnNeeded[3] &&
                ReadyDjinn.Count(d => d.Element == Element.Mercury) >= DjinnNeeded[4] &&
                Move.ValidSelection(User);
        }

        protected override List<string> InternalUse(ColossoFighter User)
        {
            if (!ValidSelection(User))
            {
                return new List<string>() { $"Not enough Djinn!" };
            }

            var PartyDjinn = User.GetTeam().SelectMany(u => u.Moves.OfType<Djinn>()).Distinct();
            var ReadyDjinn = PartyDjinn.Where(d => d.State == DjinnState.Set).OrderBy(d => d.CoolDown).ToList();
            ReadyDjinn.Where(d => d.Element == Element.Venus).Take(DjinnNeeded[1]).ToList().ForEach(d => d.Summon(User));
            ReadyDjinn.Where(d => d.Element == Element.Mars).Take(DjinnNeeded[2]).ToList().ForEach(d => d.Summon(User));
            ReadyDjinn.Where(d => d.Element == Element.Jupiter).Take(DjinnNeeded[3]).ToList().ForEach(d => d.Summon(User));
            ReadyDjinn.Where(d => d.Element == Element.Mercury).Take(DjinnNeeded[4]).ToList().ForEach(d => d.Summon(User));
            return Move.Use(User);
        }
    }
}