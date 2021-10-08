using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;

namespace IodemBot.Discords.Contexts
{
    public abstract class RequestContext
    {
        readonly SemaphoreLocker _initialLock = new SemaphoreLocker();

        private bool _initial = true;
        public async Task<bool> GetInitialAsync(bool updateAfterTouch)
        {
            return await _initialLock.LockAsync(() =>
            {
                bool val = _initial;
                if (updateAfterTouch)
                    _initial = false;

                return Task.FromResult(val);
            });
        }

        private ulong? _botOwnerId;
        public async Task<ulong> GetBotOwnerIdAsync()
        {
            if (_botOwnerId.HasValue)
                return _botOwnerId.Value;

            var application = await Client.GetApplicationInfoAsync().ConfigureAwait(false);
            _botOwnerId = application.Owner.Id;
            return _botOwnerId.Value;
        }


        public abstract DiscordSocketClient Client { get; }
        public abstract SocketGuild Guild { get; }
        public abstract ISocketMessageChannel Channel { get; }
        public abstract SocketUser User { get; }
        public abstract SocketUserMessage Message { get; }

        public ulong? GetReferenceMessageId() => Message?.Id;


        public Task<RestUserMessage> ReplyWithMessageAsync(bool ephemeral, string message = null, bool isTTS = false, Embed[] embeds = null, Embed embed = null,
            RequestOptions options = null, AllowedMentions allowedMentions = null, MessageReference messageReference = null, MessageComponent components = null, bool hasMentions = false)
            => ReplyWithMessageAsync(ephemeral ? EphemeralRule.EphemeralOrFallback : EphemeralRule.Permanent, message, isTTS, embeds, embed, options, allowedMentions, messageReference, components, hasMentions);
        public Task<RestUserMessage> ReplyWithFileAsync(bool ephemeral, Stream stream, string filename, bool isSpoiler, string message = null, bool isTTS = false, Embed[] embeds = null,
            Embed embed = null, RequestOptions options = null, AllowedMentions allowedMentions = null, MessageReference messageReference = null, MessageComponent components = null, bool hasMentions = false)
            => ReplyWithFileAsync(ephemeral ? EphemeralRule.EphemeralOrFallback : EphemeralRule.Permanent, stream, filename, isSpoiler, message, isTTS, embeds, embed, options, allowedMentions, messageReference, components, hasMentions);

        public abstract Task<RestUserMessage> ReplyWithMessageAsync(EphemeralRule ephemeralRule, string message = null, bool isTTS = false, Embed[] embeds = null, Embed embed = null, 
            RequestOptions options = null, AllowedMentions allowedMentions = null, MessageReference messageReference = null, MessageComponent components = null, bool hasMentions = false);
        public abstract Task<RestUserMessage> ReplyWithFileAsync(EphemeralRule ephemeralRule, Stream stream, string filename, bool isSpoiler, string message = null, bool isTTS = false, Embed[] embeds = null,
            Embed embed = null,  RequestOptions options = null, AllowedMentions allowedMentions = null, MessageReference messageReference = null, MessageComponent components = null, bool hasMentions = false);
        public abstract Task UpdateReplyAsync(Action<MessageProperties> propBuilder, RequestOptions options = null);


        public Task ReplyBuilderAsync(IServiceProvider baseServices, MessageBuilder messageBuilder, bool ephemeral, ulong? referenceMessageId = null)
            => ReplyBuilderAsync(baseServices, messageBuilder, ephemeral ? EphemeralRule.EphemeralOrFallback : EphemeralRule.Permanent, referenceMessageId);
        public async Task ReplyBuilderAsync(IServiceProvider baseServices, MessageBuilder messageBuilder, EphemeralRule ephemeralRule, ulong? referenceMessageId = null)
        {
            var messageData = messageBuilder.BuildOutput();

            if (messageData != null && (messageData.Embed != null || messageData.Message != null))
            {
                ephemeralRule = !messageBuilder.Success && ephemeralRule == EphemeralRule.Permanent ? EphemeralRule.EphemeralOrFallback : ephemeralRule;

                Stream stream = null;
                if (messageData.ImageStream != null)
                {
                    if (messageData.ImageStream.CanRead)
                        stream = messageData.ImageStream;
                    else if (messageData.ImageStream is MemoryStream memoryStream)
                    {
                        stream = new MemoryStream(memoryStream.ToArray());
                        stream.Seek(0, SeekOrigin.Begin);
                    }
                }

                var message = stream == null
                    ? await ReplyWithMessageAsync(ephemeralRule, messageData.Message, embed: messageData.Embed, components: messageData.Components,
                        messageReference: referenceMessageId.HasValue ? new MessageReference(referenceMessageId.Value) : null, hasMentions: messageData.HasMentions).ConfigureAwait(false)
                    : await ReplyWithFileAsync(ephemeralRule, stream, messageData.ImageFileName, messageData.ImageIsSpoiler, messageData.Message, embed: messageData.Embed,
                        components: messageData.Components, messageReference: referenceMessageId.HasValue ? new MessageReference(referenceMessageId.Value) : null,
                        hasMentions: messageData.HasMentions).ConfigureAwait(false);

                if (messageBuilder.GetDeferredMessage != null)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            using var scope = baseServices.CreateScope();
                            var innerBuilder = await messageBuilder.GetDeferredMessage.Invoke(scope.ServiceProvider, this, message);
                            if (innerBuilder != null)
                            {
                                await ReplyBuilderAsync(baseServices, innerBuilder, ephemeralRule, message.Id);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            await ReplyWithMessageAsync(ephemeralRule, "Waaaaah! Something went wrong!").ConfigureAwait(false);
                            return;
                        }
                    });
                }
            }
        }
    }
}
