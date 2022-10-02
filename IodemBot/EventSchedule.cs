using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace IodemBot
{
    public class EventSchedule
    {
        private static Dictionary<string, EventScheduleStruct> ScheduledEvents = new();

        static EventSchedule()
        {
            string json = File.ReadAllText("Resources/GoldenSun/event_schedule.json");
            var data = JsonConvert.DeserializeObject<dynamic>(json);
            ScheduledEvents = data.ToObject<Dictionary<string, EventScheduleStruct>>();
        }

        public static bool CheckEvent(string eventName)
        {
            if (ScheduledEvents.TryGetValue(eventName, out EventScheduleStruct schedule))
            {
                var today = DateTime.Today;
                return today >= schedule.Start && today <= schedule.End;
            }
            return false;
        }
    }

    public struct EventScheduleStruct
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }
}