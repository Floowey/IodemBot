using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace IodemBot.Modules.GoldenSunMechanics
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Oath
    { Solitude, Oaf, Turtle, Warrior, Mage, Venus, Mars, Jupiter, Mercury, Dispirited }

    public enum OathCompletion
    { NotCompleted = 0, Completed = 1, SolitudeCompletion = 2 }

    public class OathList
    {
        public static readonly List<Oath> ElementOaths = new() { Oath.Venus, Oath.Mars, Oath.Jupiter, Oath.Mercury };
        public static readonly List<Oath> ArchtypeOaths = new() { Oath.Mage, Oath.Warrior };
        public static readonly List<Oath> OathsWithArticle = new() { Oath.Turtle, Oath.Warrior, Oath.Mage, Oath.Dispirited };

        [JsonProperty(ItemConverterType = typeof(StringEnumConverter))]
        public List<Oath> ActiveOaths { get; set; } = new();

        [JsonProperty(ItemConverterType = typeof(StringEnumConverter))]
        public List<Oath> CompletedSolitudeOaths { get; set; } = new();

        [JsonProperty(ItemConverterType = typeof(StringEnumConverter))]
        public List<Oath> CompletedOaths { get; set; } = new();

        [JsonProperty(ItemConverterType = typeof(StringEnumConverter))]
        public List<Oath> OathsCompletedThisRun { get; set; } = new();

        public OathCompletion GetOathCompletion(Oath o)
        {
            if (CompletedSolitudeOaths.Contains(o))
                return OathCompletion.SolitudeCompletion;
            else if (CompletedOaths.Contains(o))
                return OathCompletion.Completed;
            else
                return OathCompletion.NotCompleted;
        }

        public void CompleteOaths()
        {
            if (IsOathActive(Oath.Solitude))
            {
                CompletedSolitudeOaths.AddRange(ActiveOaths);
                CompletedSolitudeOaths = CompletedSolitudeOaths.Distinct().ToList();
            }

            CompletedOaths.AddRange(ActiveOaths);
            CompletedOaths = CompletedOaths.Distinct().ToList();

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