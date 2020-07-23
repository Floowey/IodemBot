using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IodemBot.Modules;
using IodemBot.Modules.GoldenSunMechanics;

namespace IodemBot.Core.UserManagement
{
    public class Loadouts
    {
        public List<Loadout> loadouts = new List<Loadout>();

        public Loadout GetLoadout(string name)
        {
            return loadouts.FirstOrDefault(l => l.LoadoutName == name);
        }

        public void SaveLoadout(Loadout loadout)
        {
            RemoveLoadout(loadout.LoadoutName);
            loadouts.Add(loadout);
        }

        public void RemoveLoadout(string name)
        {
            loadouts.RemoveAll(l => l.LoadoutName == name);
        }
    }

    public class Loadout
    {
        public string LoadoutName { get; set; } = "";
        public Element element { get; set; } 
        public string classSeries { get; set; } = "";
        public List<string> gear { get; set; } = new List<string>();
        public List<string> djinn { get; set; } = new List<string>();

        public static Loadout GetLoadout(UserAccount account)
        {
            Loadout L = new Loadout();
            L.djinn = account.DjinnPocket.GetDjinns().Select(d => d.Name).ToList();
            L.element = account.Element;
            var classSeries = AdeptClassSeriesManager.GetClassSeries(account);
            L.classSeries = classSeries.Name;
            L.gear = account.Inv.GetGear(classSeries.Archtype).Select(i => i.Name).ToList();
            return L;
        }

        public void ApplyLoadout(UserAccount account)
        {
            var inv = account.Inv;
            GoldenSun.SetClass(account, classSeries);
            gear.ForEach(i => inv.Equip(i, AdeptClassSeriesManager.GetClassSeries(account).Archtype));
            DjinnCommands.TakeDjinn(account, djinn.ToArray());

        }
    }
}
