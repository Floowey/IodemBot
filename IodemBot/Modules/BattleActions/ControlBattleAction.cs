using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using IodemBot.ColossoBattles;
using IodemBot.Discords.Actions.Attributes;
using IodemBot.Extensions;
using IodemBot.Modules.GoldenSunMechanics;

namespace IodemBot.Modules.BattleActions
{
    public class StartBattleAction : BattleAction
    {
        public override async Task RunAsync()
        {
            _ = Battle.StartBattle();
            await Task.CompletedTask;
        }

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            var baseResult = base.CheckCustomPreconditionsAsync();
            if (!baseResult.Result.Success)
                return baseResult;

            if (Battle.Battle.SizeTeamA == 0)
                return Task.FromResult((false, "Not enough players to start."));
            return baseResult;
        }
    }

    public class JoinBattleAction : BattleAction
    {
        public Team Team { get; set; } = Team.A;

        public override async Task RunAsync()
        {
            if (Battle is PvPEnvironment pvp && Context.User is SocketGuildUser user && Team == Team.B)
            {
                await user.AddRoleAsync(pvp.TeamBRole);
                pvp.PlayersWithTeamBRole.Add(user);
            }
            _ = Battle.AddPlayer(EntityConverter.ConvertUser(Context.User), Team);
            await Task.CompletedTask;
        }

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            var baseResult = base.CheckCustomPreconditionsAsync();
            if (!baseResult.Result.Success)
                return baseResult;
            if (Battle.ContainsPlayer(Context.User.Id))
                return Task.FromResult((false, "You are already in this battle."));

            var user = EntityConverter.ConvertUser(Context.User);
            var joinResult = Battle.CanPlayerJoin(user, Team);
            if (user.Tags.Contains("EnterAllDungeons"))
                return SuccessFullResult;
            return joinResult;
        }

        public override async Task FillParametersAsync(string[] selectOptions, object[] idOptions)
        {
            if (idOptions != null && idOptions.Any())
                Team = Enum.Parse<Team>((string)idOptions.FirstOrDefault());

            await Task.CompletedTask;
        }
    }

    public class SelectMoveAction : InBattleAction
    {
        [ActionParameterComponent(Order = 0, Name = "Move", Description = "move", Required = true)]
        public string SelectedMoveName { get; set; }

        public override async Task RunAsync()
        {
            var selectedMove = Player.Moves.FirstOrDefault(m =>
                m.Name.Equals(SelectedMoveName, StringComparison.InvariantCultureIgnoreCase));

            Player.SelectedMove = selectedMove;
            var targetTeam = selectedMove.OnEnemy ? Player.Enemies : Player.Party;
            if ((selectedMove.TargetType & TargetType.NoAim) == selectedMove.TargetType || targetTeam.Count <= 1)
            {
                Player.HasSelected = true;
                selectedMove.TargetNr = 0;
            }

            if (selectedMove is Summon)
                await Context.UpdateReplyAsync(p => p.Components = ControlBattleComponents.GetSummonsComponent(Player));
            else
                await Context.UpdateReplyAsync(p =>
                    p.Components = ControlBattleComponents.GetPlayerControlComponents(Player));

            if (Player is PlayerFighter fighter) fighter.AutoTurnsInARow = 0;
            if (Player.HasSelected)
                _ = Battle.ProcessTurn(false);
            await Task.CompletedTask;
        }

        public override async Task FillParametersAsync(string[] selectOptions, object[] idOptions)
        {
            if (idOptions != null && idOptions.Any()) SelectedMoveName = (string)idOptions.FirstOrDefault();
            await Task.CompletedTask;
        }

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            var result = base.CheckCustomPreconditionsAsync();
            if (!result.GetAwaiter().GetResult().Success)
                return result;

            var selectedMove = Player.Moves.FirstOrDefault(m =>
                m.Name.Equals(SelectedMoveName, StringComparison.InvariantCultureIgnoreCase));
            if (selectedMove == null)
                return Task.FromResult((false, "Unaccessible move"));

            return result;
        }
    }

    public class SelectTargetAction : InBattleAction
    {
        [ActionParameterComponent(Order = 0, Name = "Target", Description = "target", Required = true)]
        public int SelectedTargetPosition { get; set; }

        public override async Task RunAsync()
        {
            Player.SelectedMove.TargetNr = SelectedTargetPosition;
            Player.HasSelected = true;
            if (Player.Party.Count > 1)
            { // Only update the select menu if its a group battle
                if (Player.SelectedMove is Summon)
                    await Context.UpdateReplyAsync(p => p.Components = ControlBattleComponents.GetSummonsComponent(Player));
                else
                    await Context.UpdateReplyAsync(p =>
                        p.Components = ControlBattleComponents.GetPlayerControlComponents(Player));
            }
            _ = Battle.ProcessTurn(false);
            await Task.CompletedTask;
        }

        public override async Task FillParametersAsync(string[] selectOptions, object[] idOptions)
        {
            if (selectOptions != null && selectOptions.Any())
                SelectedTargetPosition = int.Parse(selectOptions.FirstOrDefault());
            await Task.CompletedTask;
        }
    }

    public static class ControlBattleComponents
    {
        public static MessageComponent GetControlComponent(bool PvP = false)
        {
            ComponentBuilder builder = new();
            if (PvP)
            {
                builder.WithButton("Join Team A", $"{nameof(JoinBattleAction)}.A", ButtonStyle.Success,
                    Emotes.GetEmote("JoinBattle"));
                builder.WithButton("Join Team B", $"{nameof(JoinBattleAction)}.B", ButtonStyle.Success,
                    Emotes.GetEmote("JoinBattle"));
            }
            else
            {
                builder.WithButton("Join", $"{nameof(JoinBattleAction)}", ButtonStyle.Success,
                    Emotes.GetEmote("JoinBattle"));
            }

            builder.WithButton("Start", $"{nameof(StartBattleAction)}", ButtonStyle.Success,
                Emotes.GetEmote("StartBattle"));
            return builder.Build();
        }

        public static MessageComponent GetPlayerControlComponents(PlayerFighter player)
        {
            ComponentBuilder builder = new();
            foreach (var move in player.Moves)
            {
                if (move is Summon)
                    continue;
                var isSelection = player.SelectedMove == move;
                var style = move.InternalValidSelection(player) ? ButtonStyle.Primary : ButtonStyle.Secondary;
                style = isSelection ? ButtonStyle.Success : style;
                builder.WithButton($"{move.Name}{(move is Psynergy p ? $" - {p.PpCost}" : "")}",
                    $"{nameof(SelectMoveAction)}.{move.Name}", style, move.GetEmote());
            }

            if (player.SelectedMove != null)
            {
                var sm = GetTargetMenu(player);
                if (sm != null)
                    builder.WithSelectMenu(sm);
            }

            return builder.Build();
        }

        public static MessageComponent GetSummonsComponent(PlayerFighter player)
        {
            var factory = player.Factory;
            ComponentBuilder builder = new();
            foreach (var move in factory.PossibleSummons)
            {
                var isSelection = player.SelectedMove == move;
                var style = move.InternalValidSelection(player) ? ButtonStyle.Primary : ButtonStyle.Secondary;
                style = isSelection ? ButtonStyle.Success : style;

                builder.WithButton($"{move.Name}", $"{nameof(SelectMoveAction)}.{move.Name}", style, move.GetEmote());
            }

            if (player.SelectedMove != null)
            {
                var sm = GetTargetMenu(player);
                if (sm != null)
                    builder.WithSelectMenu(sm);
            }

            return builder.Build();
        }

        private static SelectMenuBuilder GetTargetMenu(PlayerFighter player)
        {
            var targets = player.SelectedMove.OnEnemy ? player.Enemies : player.Party;
            if ((player.SelectedMove.TargetType & TargetType.NoAim) == player.SelectedMove.TargetType || targets.Count <= 1)
                return null;
            var team = targets.First().Party;

            List<SelectMenuOptionBuilder> options = new();
            foreach (var f in targets)
            {
                var emote = f.IsAlive ? null : Emotes.GetEmote("Dead");

                if (f.Stats.HP != 0 && 100 * f.Stats.HP / f.Stats.MaxHP <= 10)
                    emote = Emote.Parse("<:Exclamatory:549529360604856323>");

                options.Add(new SelectMenuOptionBuilder
                {
                    Label = $"{f.Name}",
                    Value = $"{options.Count}",
                    Emote = emote,
                    IsDefault = player.HasSelected && team.IndexOf(f) == player.SelectedMove.TargetNr
                });
            }
            return new SelectMenuBuilder($"{nameof(SelectTargetAction)}", options, "Select a Target");
        }
    }
}