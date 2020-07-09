using System.Collections.Generic;
using System.Linq;
using IodemBot.Extensions;
using IodemBot.Modules.ColossoBattles;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class Summon : Move
    {
        [JsonProperty] private Move Move { get; set; }
        [JsonIgnore] public override string Name { get => Move.Name; set => Move.Name = value; }
        [JsonIgnore] public override string Emote { get => Move.Emote; set => Move.Emote = value; }
        [JsonIgnore] public override Target TargetType { get => Move.TargetType; set => Move.TargetType = value; }
        [JsonIgnore] public override List<Effect> Effects { get => Move.Effects; set => Move.Effects = value; }
        [JsonIgnore] public override int TargetNr { get => Move.TargetNr; set => Move.TargetNr = value; }
        [JsonIgnore] public override uint Range { get => Move.Range; set => Move.Range = value; }
        [JsonIgnore] public override bool HasPriority { get => Move.HasPriority; set => Move.HasPriority = value; }
        public List<Effect> EffectsOnUser = null;
        public List<Effect> EffectsOnParty = null;
        public int VenusNeeded { get; set; } = 0;
        public int MarsNeeded { get; set; } = 0;
        public int JupiterNeeded { get; set; } = 0;
        public int MercuryNeeded { get; set; } = 0;

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
            return ValidateSummon(PartyDjinn) && Move.ValidSelection(User);
        }

        public bool CanSummon(IEnumerable<Djinn> djinns)
        {
            return djinns.OfElement(Element.Venus).Count() >= VenusNeeded &&
                djinns.OfElement(Element.Mars).Count() >= MarsNeeded &&
                djinns.OfElement(Element.Jupiter).Count() >= JupiterNeeded &&
                djinns.OfElement(Element.Mercury).Count() >= MercuryNeeded;
        }

        public bool ValidateSummon(IEnumerable<Djinn> djinns)
        {
            return CanSummon(djinns.Where(d => d.State == DjinnState.Standby));
        }

        protected override List<string> InternalUse(ColossoFighter User)
        {
            if (!ValidSelection(User))
            {
                return new List<string>() { $"{User.Name} failed to summon {Emote} {Name}. Not enough Djinn!" };
            }

            var PartyDjinn = User.GetTeam().SelectMany(u => u.Moves.OfType<Djinn>()).Distinct();
            var ReadyDjinn = PartyDjinn.Where(d => d.State == DjinnState.Standby).OrderBy(d => d.Position).ToList();
            ReadyDjinn.OfElement(Element.Venus).Take(VenusNeeded).ToList().ForEach(d => d.Summon(User));
            ReadyDjinn.OfElement(Element.Mars).Take(MarsNeeded).ToList().ForEach(d => d.Summon(User));
            ReadyDjinn.OfElement(Element.Jupiter).Take(JupiterNeeded).ToList().ForEach(d => d.Summon(User));
            ReadyDjinn.OfElement(Element.Mercury).Take(MercuryNeeded).ToList().ForEach(d => d.Summon(User));
            var Text = Move.Use(User);
            if (EffectsOnUser != null)
            {
                Text.AddRange(EffectsOnUser.ApplyAll(User, User));
            }
            if (EffectsOnParty != null)
            {
                User.battle.GetTeam(User.party).ForEach(p => Text.AddRange(EffectsOnParty.ApplyAll(User, p)));
            }
            return Text;
        }

        public override string ToString()
        {
            return Move.ToString();
        }
    }
}