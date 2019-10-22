using IodemBot.Modules.ColossoBattles;
using System.Collections.Generic;
using static IodemBot.Modules.GoldenSunMechanics.Psynergy;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public enum DjinnState { Normal, Set, Recovery };

    public class Djinn : Move
    {
        private string Sprite
        {
            get
            {
                return IsShiny ? SpriteNormal : SpriteShiny;
            }
        }

        private string SpriteNormal { get; set; }
        private string SpriteShiny { get; set; }
        private bool IsShiny { get; set; } = false;
        private Element Element { get; set; }
        private Move Move { get; set; }
        private Stats Stats { get; set; }
        private DjinnState State { get; set; }

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
            return Move.ValidSelection(User);
        }

        protected override List<string> InternalUse(ColossoFighter User)
        {
            throw new System.NotImplementedException();
        }
    }
}