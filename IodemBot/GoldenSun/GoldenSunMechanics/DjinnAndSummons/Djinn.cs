using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using IodemBot.ColossoBattles;
using IodemBot.Extensions;
using Newtonsoft.Json;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public enum DjinnState
    {
        Set,
        Standby,
        Recovery
    }

    public class Djinn : Move
    {
        [JsonProperty] private Move Move { get; set; } = new Nothing();

        [JsonIgnore] public override string Name => Nickname.IsNullOrEmpty() ? Djinnname : Nickname;
        public string Nickname { get; set; } = "";
        public string Djinnname { get; set; } = "";

        [JsonIgnore]
        public override string Emote
        {
            get => Sprite;
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

        public Element Element { get; set; }

        [JsonIgnore] private string Sprite => IsShiny ? SpriteShiny : SpriteNormal;

        [JsonProperty] private string SpriteNormal { get; set; }
        [JsonProperty] private string SpriteShiny { get; set; }

        public string Event { get; set; }
        public bool IsEvent => !Event.IsNullOrEmpty();
        public bool CanBeShiny => !SpriteShiny.IsNullOrEmpty() && !IsShiny;
        public bool IsShiny { get; set; } = false;
        public Stats Stats { get; set; }

        [JsonIgnore]
        public ElementalStats ElementalStats =>
            new(
                Element == Element.Venus ? 4 : 0, Element == Element.Venus ? 4 : 0,
                Element == Element.Mars ? 4 : 0, Element == Element.Mars ? 4 : 0,
                Element == Element.Jupiter ? 4 : 0, Element == Element.Jupiter ? 4 : 0,
                Element == Element.Mercury ? 4 : 0, Element == Element.Mercury ? 4 : 0);

        [JsonIgnore]
        public DjinnState State => OnStandby ? DjinnState.Standby : CoolDown > 0 ? DjinnState.Recovery : DjinnState.Set;

        [JsonIgnore] public int CoolDown { get; set; }
        [JsonIgnore] public int Position { get; set; }

        public bool OnStandby { get; set; }

        public void UpdateMove()
        {
            Move.Emote = Sprite;
            Move.Name = Name;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            UpdateMove();
        }

        public override object Clone()
        {
            var json = JsonConvert.SerializeObject(this,
                Formatting.None,
                new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Serialize });
            return JsonConvert.DeserializeObject<Djinn>(json);
        }

        public void Summon(ColossoFighter user)
        {
            OnStandby = false;
        }

        public void Reset()
        {
            OnStandby = false;
            CoolDown = 0;
        }

        public List<string> EndTurn(ColossoFighter user)
        {
            var log = new List<string>();
            if (State == DjinnState.Recovery)
            {
                CoolDown -= 1;
                if (State == DjinnState.Set) log.Add($"{Emote} {Name} was set to {user.Name}");
            }

            return log;
        }

        public override void InternalChooseBestTarget(ColossoFighter user)
        {
            Move.ChooseBestTarget(user);
        }

        public override bool InternalValidSelection(ColossoFighter user)
        {
            return State == DjinnState.Set && Move.ValidSelection(user) && !user.HasCondition(Condition.SpiritSeal);
        }

        protected override List<string> InternalUse(ColossoFighter user)
        {
            if (user.HasCondition(Condition.SpiritSeal))
                return new List<string> { $"{user.Name} could not call upon the spirits!" };
            switch (State)
            {
                case DjinnState.Set:
                    OnStandby = true;
                    CoolDown = Math.Max(2, user.Moves.OfType<Djinn>().Max(d => d.CoolDown) + 1);
                    Position = user.Party.SelectMany(p => p.Moves.OfType<Djinn>()).Max(d => d.Position) + 1;
                    return Move.Use(user);

                case DjinnState.Standby:
                    OnStandby = false;
                    user.Moves.OfType<Djinn>().Where(d => d.CoolDown > CoolDown).ToList().ForEach(d => d.CoolDown--);
                    CoolDown = 0;
                    return new List<string> { $"{Emote} {Name} was set to {user.Name}." };

                default:
                    return new List<string>
                        {$"{user.Name} wants to summon {Emote} {Name}, but {Emote} {Name} is too tired."};
            }
        }

        public override string ToString()
        {
            return Move.ToString();
        }
    }
}