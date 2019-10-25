using IodemBot.Modules.ColossoBattles;
using System.Collections.Generic;
using System.Linq;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public enum DjinnState { Ready, Set, Recovery };

    public class Djinn : Move
    {
        public override string Name { get => Move.Name; set => Move.Name = value; }
        public override string Emote { get => Sprite; set => Move.Emote = value; }
        public override Target TargetType { get => Move.TargetType; set => Move.TargetType = value; }
        public override List<Effect> Effects { get => Move.Effects; set => Move.Effects = value; }
        public override int TargetNr { get => Move.TargetNr; set => Move.TargetNr = value; }
        public override uint Range { get => Move.Range; set => Move.Range = value; }
        public override bool HasPriority { get => Move.HasPriority; set => Move.HasPriority = value; }
        public Element Element { get; set; }

        private string Sprite => IsShiny ? SpriteNormal : SpriteShiny;

        private string SpriteNormal { get; set; }
        private string SpriteShiny { get; set; }
        private bool IsShiny { get; set; } = false;
        private Move Move { get; set; }
        public Stats Stats { get; set; }

        public DjinnState State => IsSet ? DjinnState.Set : (CoolDown > 0 ? DjinnState.Recovery : DjinnState.Ready);

        public int CoolDown { get; private set; }
        private bool IsSet { get; set; }

        public override object Clone()
        {
            throw new System.NotImplementedException();
        }

        public void Summon(ColossoFighter User)
        {
            IsSet = false;
            if (CoolDown == 0)
            {
                CoolDown = User.Moves.OfType<Djinn>().Max(d => d.CoolDown) + 1;
            }
        }

        public override void InternalChooseBestTarget(ColossoFighter User)
        {
            Move.ChooseBestTarget(User);
        }

        public override bool InternalValidSelection(ColossoFighter User)
        {
            return State == DjinnState.Ready && Move.ValidSelection(User);
        }

        protected override List<string> InternalUse(ColossoFighter User)
        {
            switch (State)
            {
                case DjinnState.Ready:
                    IsSet = true;
                    CoolDown = User.Moves.OfType<Djinn>().Max(d => d.CoolDown) + 1;
                    return Move.Use(User);

                case DjinnState.Set:
                    IsSet = false;
                    CoolDown = 0;
                    return new List<string>() { $"{Name} was set to {User.Name}." };

                default:
                case DjinnState.Recovery:
                    return new List<string>() { $"{Name} is too tired." };
            }
        }
    }
}