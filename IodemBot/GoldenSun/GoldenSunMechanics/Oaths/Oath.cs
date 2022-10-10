using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IodemBot.Modules.GoldenSunMechanics
{
    public enum Oath
    { Solitude, Oaf, Idleness, Warrior, Mage, Venus, Mars, Jupiter, Mercury }

    public enum OathCompletion
    { NotCompleted = 0, Active = 1, Completed = 2, SolitudeCompletion = 3 }

    public class OathList
    {
        private static readonly List<Oath> ElementOaths = new() { Oath.Venus, Oath.Mars, Oath.Jupiter, Oath.Mercury };
        public List<Oath> ActiveOaths { get; set; } = new();
        public List<Oath> CompletedSolitudeOaths { get; set; } = new();
        public List<Oath> CompletedOaths { get; set; } = new();
        public List<Oath> OathsCompletedThisRun { get; set; } = new();

        public OathCompletion GetOathCompletion(Oath o)
        {
            if (CompletedSolitudeOaths.Contains(o))
                return OathCompletion.SolitudeCompletion;
            else if (CompletedOaths.Contains(o))
                return OathCompletion.Completed;
            else if (ActiveOaths.Contains(o))
                return OathCompletion.Active;
            else
                return OathCompletion.NotCompleted;
        }

        public void CompleteOaths()
        {
            if (IsOathActive(Oath.Solitude))
            {
                CompletedSolitudeOaths.AddRange(ActiveOaths);
                CompletedSolitudeOaths = (List<Oath>)CompletedSolitudeOaths.Distinct();
            }
            CompletedOaths.AddRange(ActiveOaths);
            CompletedOaths = (List<Oath>)CompletedOaths.Distinct();
            OathsCompletedThisRun.AddRange(ActiveOaths);
            ActiveOaths.Clear();
        }

        public bool IsOathActive(Oath o)
        {
            return ActiveOaths.Contains(o);
        }

        public bool IsOathOfElementActive()
        {
            return ElementOaths.Any(e => ActiveOaths.Contains(e));
        }
    }
}