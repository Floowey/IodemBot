using System;
using System.Collections.Generic;
using System.Linq;
using IodemBot.Modules;
using IodemBot.Modules.GoldenSunMechanics;

namespace IodemBot.Core.UserManagement
{
    public class Loadouts
    {
        public List<Loadout> LoadoutsList { get; set; } = new();

        public Loadout GetLoadout(string name)
        {
            return LoadoutsList.FirstOrDefault(l =>
                l.LoadoutName.Equals(name, StringComparison.CurrentCultureIgnoreCase));
        }

        public void SaveLoadout(Loadout loadout)
        {
            RemoveLoadout(loadout.LoadoutName);
            LoadoutsList.Add(loadout);
        }

        public void RemoveLoadout(string name)
        {
            LoadoutsList.RemoveAll(l => l.LoadoutName.Equals(name, StringComparison.CurrentCultureIgnoreCase));
        }
    }

    public class Loadout
    {
        public string LoadoutName { get; set; } = "";
        public Element Element { get; set; } = Element.None;
        public string ClassSeries { get; set; } = "";
        public List<string> Gear { get; set; } = new();
        public List<string> Djinn { get; set; } = new();
        public string Passive {get;set;} = "";


        public static Loadout GetLoadout(UserAccount account)
        {
            var l = new Loadout
            {
                Djinn = account.DjinnPocket.GetDjinns().Select(d => d.Name).ToList(),
                Element = account.Element
            };
            var classSeries = account.ClassSeries;
            l.ClassSeries = account.ClassSeries.Name;
            l.Gear = account.Inv.GetGear(classSeries.Archtype).OrderBy(i => i.ItemType).Select(i => i.Name).ToList();
            l.Passive = account.Passives.SelectedPassive;
            return l;
        }

        public void ApplyLoadout(UserAccount account)
        {
            var inv = account.Inv;
            GoldenSunCommands.SetClass(account, ClassSeries);
            Gear.ForEach(i => inv.Equip(i, account.ClassSeries.Archtype));
            DjinnCommands.TakeDjinn(account, Djinn.ToArray());
            account.Passives.SelectedPassive = Passive;
        }
    }
}