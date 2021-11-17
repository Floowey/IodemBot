using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;

namespace IodemBot.Discords.Actions
{
    public class ActionAutoCompleteProperties
    {
        public Func<AutocompleteOption, IReadOnlyCollection<AutocompleteOption>, IEnumerable<AutocompleteResult>> AutoComplete { get; set; }
    }
}