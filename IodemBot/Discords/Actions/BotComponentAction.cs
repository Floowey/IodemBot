using System.Threading.Tasks;

namespace IodemBot.Discords.Actions
{
    public abstract class BotComponentAction : BotAction
    {
        public virtual Task FillParametersAsync(string[] selectOptions, object[] idOptions) => Task.CompletedTask;
    }
}
