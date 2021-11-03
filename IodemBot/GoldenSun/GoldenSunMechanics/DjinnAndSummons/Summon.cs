using System.Collections.Generic;
using System.Linq;
using IodemBot.ColossoBattles;
using IodemBot.Extensions;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class Summon : Move
    {
        public List<Effect> EffectsOnParty = null;
        public List<Effect> EffectsOnUser = null;
        [JsonProperty] private Move Move { get; set; } = new Nothing();

        [JsonIgnore]
        public override string Name
        {
            get => Move.Name;
            set => Move.Name = value;
        }

        [JsonIgnore]
        public override string Emote
        {
            get => Move.Emote;
            set => Move.Emote = value;
        }

        [JsonIgnore]
        public override TargetType TargetType
        {
            get => Move.TargetType;
            set => Move.TargetType = value;
        }

        [JsonIgnore]
        public override List<Effect> Effects
        {
            get => Move.Effects;
            set => Move.Effects = value;
        }

        [JsonIgnore]
        public override int TargetNr
        {
            get => Move.TargetNr;
            set => Move.TargetNr = value;
        }

        [JsonIgnore]
        public override uint Range
        {
            get => Move.Range;
            set => Move.Range = value;
        }

        [JsonIgnore]
        public override bool HasPriority
        {
            get => Move.HasPriority;
            set => Move.HasPriority = value;
        }

        public int VenusNeeded { get; set; } = 0;
        public int MarsNeeded { get; set; } = 0;
        public int JupiterNeeded { get; set; } = 0;
        public int MercuryNeeded { get; set; } = 0;

        public override object Clone()
        {
            return DjinnAndSummonsDatabase.GetSummon(Name);
        }

        public override void InternalChooseBestTarget(ColossoFighter user)
        {
            Move.ChooseBestTarget(user);
        }

        public override bool InternalValidSelection(ColossoFighter user)
        {
            var partyDjinn = user.Party.SelectMany(u => u.Moves.OfType<Djinn>()).Distinct();
            return ValidateSummon(partyDjinn) && Move.ValidSelection(user) && !user.HasCondition(Condition.SpiritSeal);
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

        protected override List<string> InternalUse(ColossoFighter user)
        {
            if (user.HasCondition(Condition.SpiritSeal))
                return new List<string> { $"{user.Name} could not call upon the spirits!" };
            if (!ValidSelection(user))
                return new List<string> { $"{user.Name} failed to summon {Emote} {Name}. Not enough Djinn!" };
            var log = new List<string>();
            var t = Validate(user);
            log.AddRange(t.Log);
            if (!t.IsValid) return log;
            if (Move.ValidSelection(user))
            {
                var partyDjinn = user.Party.SelectMany(u => u.Moves.OfType<Djinn>()).Distinct();
                var readyDjinn = partyDjinn.Where(d => d.State == DjinnState.Standby).OrderBy(d => d.Position).ToList();
                readyDjinn.OfElement(Element.Venus).Take(VenusNeeded).ToList().ForEach(d => d.Summon(user));
                readyDjinn.OfElement(Element.Mars).Take(MarsNeeded).ToList().ForEach(d => d.Summon(user));
                readyDjinn.OfElement(Element.Jupiter).Take(JupiterNeeded).ToList().ForEach(d => d.Summon(user));
                readyDjinn.OfElement(Element.Mercury).Take(MercuryNeeded).ToList().ForEach(d => d.Summon(user));
                log.AddRange(Move.Use(user));
            }
            else
            {
                return log;
            }

            if (EffectsOnUser != null) log.AddRange(EffectsOnUser.ApplyAll(user, user));
            if (EffectsOnParty != null)
                user.Battle.GetTeam(user.party).ForEach(p => log.AddRange(EffectsOnParty.ApplyAll(user, p)));
            return log;
        }

        public override string ToString()
        {
            return Move.ToString();
        }
    }
}