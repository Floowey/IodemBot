using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using IodemBot.ColossoBattles;
using IodemBot.Modules.GoldenSunMechanics;
using IodemBot.Discords.Actions.Attributes;
using IodemBot.Extensions;

namespace IodemBot.Modules.BattleActions
{
    public class StartBattleAction : BattleAction
    {
        public override async Task RunAsync()
        {
            _ = battle.StartBattle();
            await Task.CompletedTask;
        }

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            var baseResult = base.CheckCustomPreconditionsAsync();
            if (!baseResult.Result.Success)
                return baseResult;

            if (!battle.IsActive)
                return Task.FromResult((false, "No Players in Battle"));
            return baseResult;
        }
    }

    public class JoinBattleAction : BattleAction
    {

        public string Team { get; set; } = "A";
        public override async Task RunAsync()
        {
            _ = battle.AddPlayer(EntityConverter.ConvertUser(Context.User));
            await Task.CompletedTask;
        }        

        protected override Task<(bool Success, string Message)> CheckCustomPreconditionsAsync()
        {
            var baseResult = base.CheckCustomPreconditionsAsync();
            if (!baseResult.Result.Success)
                return baseResult;

          
            return baseResult;
        }
    }

    public class SelectMoveAction : InBattleAction
    {
        [ActionParameterComponent(Order =0, Name ="Move", Description ="move", Required =true)]
        public string SelectedMoveName { get; set; }
        public override async Task RunAsync()
        {
            var SelectedMove = player.Moves.FirstOrDefault(m => m.Name.Equals(SelectedMoveName, StringComparison.InvariantCultureIgnoreCase));

            player.SelectedMove = SelectedMove;
            if (SelectedMove.TargetType == TargetType.PartySelf ||
                SelectedMove.TargetType == TargetType.PartyAll ||
                (SelectedMove.TargetType == TargetType.PartySingle && player.Party.Count == 1) ||
                ((SelectedMove.TargetType == TargetType.EnemyRange || SelectedMove.TargetType == TargetType.EnemyAll) && player.Enemies.Count == 1))
            {
                player.hasSelected = true;
                SelectedMove.TargetNr = 0;
            }

            if(SelectedMove is Summon s)
            {
                await Context.UpdateReplyAsync(p => p.Components = ControlBattleComponents.GetSummonsComponent(player));
            } else
            {
                await Context.UpdateReplyAsync(p => p.Components = ControlBattleComponents.GetPlayerControlComponents(player));
            }

            if (player is PlayerFighter fighter)
            {
                fighter.AutoTurnsInARow = 0;
            }
            if(player.hasSelected)
                _ = battle.ProcessTurn(false);
            await Task.CompletedTask;
        }
        public override async Task FillParametersAsync(string[] selectOptions, object[] idOptions)
        {
            if (idOptions != null && idOptions.Any())
            {
                SelectedMoveName = (string)idOptions.FirstOrDefault();
            }
            await Task.CompletedTask;
        }
    }

    public class SelectTargetAction : InBattleAction
    {
        [ActionParameterComponent(Order = 0, Name = "Target", Description = "target", Required = true)]
        public int SelectedTargetPosition { get; set; }
        public override async Task RunAsync()
        {
            player.SelectedMove.TargetNr = SelectedTargetPosition;
            player.hasSelected = true;
            _ = battle.ProcessTurn(false);
            await Task.CompletedTask;
        }

        public override async Task FillParametersAsync(string[] selectOptions, object[] idOptions)
        {
            if (selectOptions != null && selectOptions.Any())
            {
                SelectedTargetPosition = int.Parse(selectOptions.FirstOrDefault());
            }
            await Task.CompletedTask;
        }
    }

    public static class ControlBattleComponents
    {
        public static MessageComponent GetControlComponent(bool PvP = false)
        {
            ComponentBuilder builder = new();
            builder.WithButton("Join", $"{nameof(JoinBattleAction)}", ButtonStyle.Success);
            builder.WithButton("Start", $"{nameof(StartBattleAction)}", ButtonStyle.Success);
            return builder.Build();
        }

        public static MessageComponent GetPlayerControlComponents(PlayerFighter player)
        {
            ComponentBuilder builder = new();
            foreach (var move in player.Moves)
            {
                if (move is Summon)
                    continue; ;
                bool isSelection = player.SelectedMove == move;
                ButtonStyle style = isSelection ? ButtonStyle.Primary : ButtonStyle.Secondary;
                style = move.InternalValidSelection(player) ? style : ButtonStyle.Danger;
                builder.WithButton(label: $"{move.Name}", customId: $"{nameof(SelectMoveAction)}.{move.Name}", style:style , emote: move.GetEmote());
            }

            if (!player.hasSelected && player.SelectedMove != null)
            {
                List<SelectMenuOptionBuilder> options = new();
                var Team = player.SelectedMove.TargetType == TargetType.PartySingle ? player.Party : player.Enemies;
                foreach (var f in Team)
                {
                    options.Add(new() { Label = $"{f.Name}", Value = $"{options.Count}",Emote = f.IsAlive?null: Emotes.GetEmote("Dead") });
                }
                builder.WithSelectMenu($"{nameof(SelectTargetAction)}" , options, "Select a Target", disabled: player.hasSelected);
            }

            return builder.Build();
        }

        public static MessageComponent GetSummonsComponent(PlayerFighter player)
        {
            var factory = player.factory;
            ComponentBuilder builder = new();
            foreach (var move in factory.PossibleSummons)
            {
                bool isSelection = player.SelectedMove == move;
                ButtonStyle style = isSelection ? ButtonStyle.Primary : ButtonStyle.Secondary;
                style = move.InternalValidSelection(player) ? style : ButtonStyle.Danger;

                builder.WithButton(label: $"{move.Name}", customId: $"{nameof(SelectMoveAction)}.{move.Name}", style: style, emote: move.GetEmote());
            }

            if (!player.hasSelected && player.SelectedMove != null)
            {
                List<SelectMenuOptionBuilder> options = new();
                var Team = player.SelectedMove.TargetType == TargetType.PartySingle ? player.Party : player.Enemies;
                foreach (var f in Team)
                {
                    options.Add(new() { Label = $"{f.Name}", Value = $"{options.Count}" });
                }
                builder.WithSelectMenu($"{nameof(SelectTargetAction)}", options, "Select a Target", disabled: player.hasSelected);
            }
            return builder.Build();
        }
    }
}
