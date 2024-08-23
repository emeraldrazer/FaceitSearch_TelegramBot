#pragma warning disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Faceit_TelegramBot.Model
{
    public class ResultHub
    {
        public string id { get; set; }
        public string guid { get; set; }
        public string name { get; set; }
        public string region { get; set; }
        public OrganizerHub organizer { get; set; }
        public int minSkillLevel { get; set; }
        public int maxSkillLevel { get; set; }
        public int membersCount { get; set; }
        public string join { get; set; }
        public string? avatar { get; set; }
    }

    public class OrganizerHub
    {
        public string id { get; set; }
        public string type { get; set; }
        public string name { get; set; }
    }
}
