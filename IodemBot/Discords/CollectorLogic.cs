using System;
using System.Threading.Tasks;
using Discord;
using IodemBot.Discords.Services;

namespace IodemBot.Discords
{
    public class CollectorLogic
    {
        private SemaphoreLocker _resultLock = new SemaphoreLocker();
        private bool _enabled = true;
        private Func<IUser, ulong, object[], object[], Task<MessageBuilder>> _execute;

        public ulong MessageId { get; set; }
        public ulong OriginalUserId { get; set; }
        public bool OnlyOriginalUserAllowed { get; set; }

        public Func<IUser, ulong, object[], object[], Task<MessageBuilder>> Execute
        {
            get => _execute;
            set
            {
                if (value == null)
                    _execute = null;
                else
                {
                    _execute = async (userData, messageId, idOptions, selectOptions) =>
                    {
                        return await _resultLock.LockAsync(async () =>
                        {
                            if (_enabled)
                                return await value(userData, messageId, idOptions, selectOptions);

                            return new MessageBuilder(userData, "Sorry! You were just too late!", false, null);
                        });
                    };
                }
            }
        }

        public async Task ExecuteAndWait(ActionService actionService, int secondsToWait)
        {
            try
            {
                actionService.RegisterCollector(this);
                await Task.Delay(TimeSpan.FromSeconds(secondsToWait));
            }
            finally
            {
                await _resultLock.LockAsync(() =>
                {
                    _enabled = false;
                    actionService.UnregisterCollector(this);
                    return Task.CompletedTask;
                });
            }
        }
    }
}