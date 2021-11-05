using System;
using System.Collections.Generic;
using System.Linq;
using IodemBot.Core.UserManagement;
using IodemBot.Extensions;

namespace IodemBot.ColossoBattles
{
    public class ColossoBattle
    {
        public List<ColossoFighter> TeamA = new();
        public List<ColossoFighter> TeamB = new();
        public bool IsActive { get; set; }
        public bool TurnActive { get; set; }
        public int TurnNumber { get; set; }

        public int SizeTeamA => TeamA.Count;

        public int SizeTeamB => TeamB.Count;

        public List<string> Log { get; set; } = new();
        public int OutValue { get; set; } = -1;

        public List<ColossoFighter> GetTeam(Team team)
        {
            return team == Team.A ? TeamA : TeamB;
        }

        public void Start()
        {
            IsActive = true;
            TurnNumber = 0;
            Log.Clear();
            foreach (var p in TeamA.Concat(TeamB).ToList())
            {
                if (p is NpcEnemy) p.SelectRandom();

                if (p is PlayerFighter fighter)
                {
                    fighter.BattleStats = new BattleStats();
                    fighter.BattleStats.TotalTeamMates += TeamA.Count - 1;
                    if (TeamA.Count == 1) fighter.BattleStats.SoloBattles++;
                }
            }
        }

        public bool ForceTurn()
        {
            if (TurnActive) return false;

            Console.WriteLine("Forcing turn.");
            var fighters = new List<ColossoFighter>(TeamA);
            fighters.AddRange(TeamB);
            fighters.ForEach(f =>
            {
                if (!f.HasSelected)
                {
                    f.SelectRandom();
                    if (f is PlayerFighter player) player.AutoTurnsInARow++;
                }
            });
            return Turn();
        }

        public void AddPlayer(ColossoFighter player, Team team)
        {
            if (IsActive) return;

            if (TeamA.Any(p => !p.ImgUrl.IsNullOrEmpty() && p.ImgUrl == player.ImgUrl)) return;
            GetTeam(team).Add(player);
            player.party = team;
            player.Battle = this;
        }

        public bool Turn()
        {
            if (!IsActive) return false;

            if (TurnActive) return false;

            if (!(TeamA.All(p => p.HasSelected) && TeamB.All(p => p.HasSelected))) return false;
            Log.Clear();
            OutValue = -1;
            if (SizeTeamB == 0 || SizeTeamA == 0)
            {
                Console.WriteLine("The stupid bug happened");
                Log.Add("Error occured. You win.");
                IsActive = false;
                return true;
            }

            TurnActive = true;
            Log.Add($"Turn {++TurnNumber}");

            try
            {
                StartTurn(); // moves with priority
                MainTurn();
                ExtraTurn(); // extra Turns
                EndTurn();
            }
            catch (Exception e)
            {
                Log.Add(e.ToString());
                Console.WriteLine("Turn Processing Error: " + e);
            }

            //Check for Game Over
            if (GameOver()) IsActive = false;
            TurnActive = false;
            return true;
        }

        private void StartTurn()
        {
            var fighters = TeamA.Concat(TeamB).ToList();
            fighters.Shuffle();
            fighters.OrderByDescending(f => f.Stats.Spd * f.MultiplyBuffs("Speed"))
                .ToList()
                .ForEach(f => Log.AddRange(f.StartTurn()));
        }

        private void MainTurn()
        {
            var fighters = TeamA.Concat(TeamB).ToList();
            fighters.Shuffle();
            fighters
                .OrderByDescending(f => f.Stats.Spd * f.MultiplyBuffs("Speed"))
                .ToList()
                .ForEach(f => Log.AddRange(f.MainTurn()));
        }

        private void ExtraTurn()
        {
            var fighters = TeamA.Concat(TeamB).ToList();
            fighters.Shuffle();
            fighters
                .OrderByDescending(f => f.Stats.Spd * f.MultiplyBuffs("Speed"))
                .ToList()
                .ForEach(f => Log.AddRange(f.ExtraTurn()));
        }

        private void EndTurn()
        {
            var fighters = TeamA.Concat(TeamB).ToList();
            fighters.Shuffle();
            fighters.OrderByDescending(f => f.Stats.Spd * f.MultiplyBuffs("Speed"))
                .ToList()
                .ForEach(f => Log.AddRange(f.EndTurn()));
            TeamA.RemoveAll(m => m.Name == "Runner");
            TeamB.RemoveAll(m => m.Name == "Runner");
        }

        private bool GameOver()
        {
            return !(TeamA.Any(p => p.IsAlive) && TeamB.Any(p => p.IsAlive));
        }

        public Team GetWinner()
        {
            return TeamA.Any(p => p.IsAlive) ? Team.A : Team.B;
        }
    }
}