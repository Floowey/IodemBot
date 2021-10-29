using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using IodemBot.ColossoBattles;
using IodemBot.Extensions;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public enum DjinnState { Set, Standby, Recovery };

    public class Djinn : Move
    {
        [JsonProperty] private Move Move { get; set; } = new Nothing();
        public override string Name { get => Nickname.IsNullOrEmpty() ? Djinnname : Nickname; }
        public string Nickname { get; set; } = "";
        public string Djinnname { get; set; } = "";
        [JsonIgnore] public override string Emote { get => Sprite; set => Move.Emote = value; }
        [JsonIgnore] public override TargetType TargetType { get => Move.TargetType; set => Move.TargetType = value; }
        [JsonIgnore] public override List<Effect> Effects { get => Move.Effects; set => Move.Effects = value; }
        [JsonIgnore] public override int TargetNr { get => Move.TargetNr; set => Move.TargetNr = value; }
        [JsonIgnore] public override uint Range { get => Move.Range; set => Move.Range = value; }
        [JsonIgnore] public override bool HasPriority { get => Move.HasPriority; set => Move.HasPriority = value; }
        public Element Element { get; set; }

        [JsonIgnore] private string Sprite => IsShiny ? SpriteShiny : SpriteNormal;

        [JsonProperty] private string SpriteNormal { get; set; }
        [JsonProperty] private string SpriteShiny { get; set; }

        public string Event { get; set; }
        public bool IsEvent { get => !Event.IsNullOrEmpty(); }
        public bool CanBeShiny { get => !SpriteShiny.IsNullOrEmpty() && !IsShiny; }
        public bool IsShiny { get; set; } = false;
        public Stats Stats { get; set; }

        public void UpdateMove()
        {
            Move.Emote = Sprite;
            Move.Name = Name;
        }

        [JsonIgnore]
        public ElementalStats ElementalStats
        {
            get => new ElementalStats(
                Element == Element.Venus ? 4 : 0, Element == Element.Venus ? 4 : 0,
                Element == Element.Mars ? 4 : 0, Element == Element.Mars ? 4 : 0,
                Element == Element.Jupiter ? 4 : 0, Element == Element.Jupiter ? 4 : 0,
                Element == Element.Mercury ? 4 : 0, Element == Element.Mercury ? 4 : 0);
        }

        [JsonIgnore] public DjinnState State => IsSet ? DjinnState.Standby : (CoolDown > 0 ? DjinnState.Recovery : DjinnState.Set);
        [JsonIgnore] public int CoolDown { get; set; }
        [JsonIgnore] public int Position { get; set; }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            UpdateMove();
        }

        private bool IsSet { get; set; }

        public override object Clone()
        {
            return JsonConvert.DeserializeObject<Djinn>(JsonConvert.SerializeObject(this));
        }

        public void Summon(ColossoFighter User)
        {
            IsSet = false;
        }

        public void Reset()
        {
            IsSet = false;
            CoolDown = 0;
        }

        public List<string> EndTurn(ColossoFighter User)
        {
            var log = new List<string>();
            if (State == DjinnState.Recovery)
            {
                CoolDown -= 1;
                if (State == DjinnState.Set)
                {
                    log.Add($"{Emote} {Name} was set to {User.Name}");
                }
            }
            return log;
        }

        public override void InternalChooseBestTarget(ColossoFighter User)
        {
            Move.ChooseBestTarget(User);
        }

        public override bool InternalValidSelection(ColossoFighter User)
        {
            return State == DjinnState.Set && Move.ValidSelection(User) && !User.HasCondition(Condition.SpiritSeal);
        }

        protected override List<string> InternalUse(ColossoFighter User)
        {
            if (User.HasCondition(Condition.SpiritSeal))
            {
                return new List<string>() { $"{User.Name} could not call upon the spirits!" };
            }
            switch (State)
            {
                case DjinnState.Set:
                    IsSet = true;
                    CoolDown = Math.Max(2,User.Moves.OfType<Djinn>().Max(d => d.CoolDown) + 1);
                    Position = User.Party.SelectMany(p => p.Moves.OfType<Djinn>()).Max(d => d.Position) + 1;
                    return Move.Use(User);

                case DjinnState.Standby:
                    IsSet = false;
                    User.Moves.OfType<Djinn>().Where(d => d.CoolDown > CoolDown).ToList().ForEach(d => d.CoolDown--);
                    CoolDown = 0;
                    return new List<string>() { $"{Emote} {Name} was set to {User.Name}." };

                case DjinnState.Recovery:
                default:
                    return new List<string>() { $"{User.Name} wants to summon {Emote} {Name}, but {Emote} {Name} is too tired." };
            }
        }

        public override string ToString()
        {
            return Move.ToString();
        }
    }
}