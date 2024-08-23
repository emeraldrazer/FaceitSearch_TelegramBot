#pragma warning disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Faceit_TelegramBot.Model
{
    public class ResultTour
    {
        public string id { get; set; }
        public string name{ get; set; }
        public string region { get; set; }
        public string type { get; set; }
        public string status { get; set; }
        public OrganizerHub organizer { get; set; }
        public Prize[] prizes { get; set; }
        public string guid { get; set; }
        public string start_date { get; set; }
        public int number_of_players_joined { get; set; }
        public int number_of_players_checkedin { get; set; }
        public int number_of_players { get; set; }
        public int skill_level_min { get; set; }
        public int skill_level_max { get; set; }
        public string join { get; set; }
        public string membership_type { get; set; }
        public int team_size { get; set; }
        public int min_skill { get; set; }
        public int max_skill { get; set; }
        public string prize_type { get; set; }
        public int total_prize_label { get; set; }
        public int number_of_players_participants { get; set; }
        public int subscriptions_count { get; set; }
    }

    public class Prize
    {
        public string type { get; set; }
        public int amount { get; set; }
    }
}
