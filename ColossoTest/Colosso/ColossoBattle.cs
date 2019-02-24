using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColossoTest.Colosso
{
    public class ColossoBattle
    {
        public enum Team { A,B}
        //public static NPCEnemy enemy;
        public List<ColossoFighter> TeamA = new List<ColossoFighter>();
        public List<ColossoFighter> TeamB = new List<ColossoFighter>();
        public bool isActive = false;
        public uint sizeTeamA = 1;
        public uint sizeTeamB = 4;
        public List<string> log = new List<string>();

        public ColossoBattle()
        {
            
        }

        public void Start()
        {
            isActive = true;
        }

        public void TimeUp()
        {
            // Select random Move from players movepools

            // Force Next Turn
            Turn();
        }

        public bool Turn()
        {
            //Somehow build "log" of turn and return it.
            //Check if every players have selected their Move

            if (!isActive) return false;

            TeamA.ForEach(p =>
            {
                if (p is NPCEnemy) p.selectRandom();
            });

            TeamB.ForEach(p =>
            {
                if (p is NPCEnemy) p.selectRandom();
            });

            log.Clear();
            if(TeamA.Aggregate(false, (p, s) => !s.hasSelected ? true : p) ||
                TeamB.Aggregate(false, (p, s) => !s.hasSelected ? true : p))
            {
                return false;
            }
            bool b = true;
            //Stop Timer, just in Case

            //Start Turn for things like Defend
            StartTurn();

            //Main Turn
            log.AddRange(MainTurn());

            //End Turn
            EndTurn();

            //Check for Game Over
            if (gameOver()){
                isActive = false;
            }
            return b;
        }

        public void AddPlayer(ColossoFighter player, Team team)
        {
            if (isActive) return;
            if(TeamA.Contains(player) || TeamB.Contains(player))
            {
                return;
            }
            
            if (team == Team.A){
                TeamA.Add(player);
                player.party = Team.A;
                player.enemies = Team.B;
            } else
            {
                TeamB.Add(player);
                player.party = Team.B;
                player.enemies = Team.A;
            }

            player.battle = this;
        }

        public List<ColossoFighter> getTeam(Team team)
        {
            if (team == Team.A)
            {
                return TeamA;
            } else
            {
                return TeamB;
            }
        }

        private void StartTurn()
        {
            List<ColossoFighter> fighters = new List<ColossoFighter>(TeamA);
            fighters.AddRange(TeamB);
            fighters.OrderBy(f => f.stats.Spd);
            fighters.ForEach(f => f.StartTurn());
        }

        private List<string> MainTurn()
        {
            List<string> turnLog = new List<string>();
            List<ColossoFighter> fighters = new List<ColossoFighter>(TeamA);
            fighters.AddRange(TeamB);
            //fighters.Sort();
            fighters = fighters.OrderByDescending(f => f.stats.Spd).ToList();
            fighters.ForEach(f => { turnLog.AddRange(f.MainTurn());});
            return turnLog;
        }

        private void EndTurn()
        {
            List<ColossoFighter> fighters = new List<ColossoFighter>(TeamA);
            fighters.AddRange(TeamB);
            fighters.OrderBy(f => f.stats.Spd);
            fighters.ForEach(f => f.EndTurn());
        }

        public void resetGame()
        {
            TeamA = new List<ColossoFighter>();
            TeamB = new List<ColossoFighter>();
            isActive = false;
        }

        private bool gameOver()
        {
            return !TeamA.Where(p => p.IsAlive()).Any() || !TeamB.Where(p => p.IsAlive()).Any();
        }

        public Team getWinner()
        {
            if (TeamA.Where(p => p.IsAlive()).Any()) return Team.A; else return Team.B;
        }
    }
}
