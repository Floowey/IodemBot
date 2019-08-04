using System.Collections.Generic;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public class Moveset
    {
        public static Move[] GetMoveSet(string[] moveNames)
        {
            List<Move> moves = new List<Move> { new Attack(), new Defend() };
            foreach (string s in moveNames)
            {
                Move m = PsynergyDatabase.GetPsynergy(s);
                moves.Add(m);
            }
            return moves.ToArray();
        }
    }
}