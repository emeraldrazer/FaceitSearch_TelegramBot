#pragma warning disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Faceit_TelegramBot.Model
{
    public class SearchResponseModel
    {
        public Payload payload { get; set; }
        public long time { get; set; }
        public string env { get; set; }
        public string version { get; set; }
        public string url { get; set; }
    }

    public class Payload
    {
        public int offset { get; set; }
        public int limit { get; set; }
        public Organizer organizers { get; set; }
        public Hub hubs { get; set; }
        public Tournament tournaments { get; set; }
        public Player players { get; set; }
        public Team teams { get; set; }
    }

    public class Organizer
    {
        public ResultOrg[]? results { get; set; }
        public int total_count { get; set; }
    }

    public class Hub
    {
        public ResultHub[]? results { get; set; }
        public int total_count { get; set; }
    }

    public class Tournament
    {
        public ResultTour[]? results { get; set; }
        public int total_count { get; set; }
    }

    public class Player
    {
        public ResultPlayer[]? results { get; set; }
        public int total_count { get; set; }
    }

    public class Team
    {
        public ResultTeam[]? results { get; set; }
        public int total_count { get; set; }
    }
}
