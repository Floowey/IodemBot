using System;
using System.Collections.Generic;
using System.Linq;
using IodemBot.Modules;
using IodemBot.Modules.GoldenSunMechanics;

namespace IodemBot.Core.UserManagement
{
    public class Loadouts
    {
        public List<Loadout> loadouts { get; set; } = new List<Loadout>();

        public Loadout GetLoadout(string name)
        {
            return loadouts.FirstOrDefault(l => l.LoadoutName.Equals(name, StringComparison.CurrentCultureIgnoreCase));
        }

        public void SaveLoadout(Loadout loadout)
        {
            RemoveLoadout(loadout.LoadoutName);
            loadouts.Add(loadout);
        }

        public void RemoveLoadout(string name)
        {
            loadouts.RemoveAll(l => l.LoadoutName.Equals(name, StringComparison.CurrentCultureIgnoreCase));
        }
    }

    public class Loadout
    {
        public string LoadoutName { get; set; } = "";
        public Element Element { get; set; } = Element.none;
        public string ClassSeries { get; set; } = "";
        public List<string> Gear { get; set; } = new List<string>();
        public List<string> Djinn { get; set; } = new List<string>();

        public static Loadout GetLoadout(UserAccount account)
        {
            Loadout L = new Loadout
            {
                Djinn = account.DjinnPocket.GetDjinns().Select(d => d.Name).ToList(),
                Element = account.Element
            };
            var classSeries = AdeptClassSeriesManager.GetClassSeries(account);
            L.ClassSeries = classSeries.Name;
            L.Gear = account.Inv.GetGear(classSeries.Archtype).OrderBy(i => i.ItemType).Select(i => i.Name).ToList();
            return L;
        }

        public void ApplyLoadout(UserAccount account)
        {
            var inv = account.Inv;
            GoldenSunCommands.SetClass(account, ClassSeries);
            Gear.ForEach(i => inv.Equip(i, AdeptClassSeriesManager.GetClassSeries(account).Archtype));
            DjinnCommands.TakeDjinn(account, Djinn.ToArray());
        }
    }
}
