#pragma warning disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Faceit_TelegramBot.Model
{
    public class ResultTeam
    {
        public string id { get; set; }
        public string guid { get; set; }
        public string name { get; set; }
        public string game { get; set; }
        public string tag { get; set; }
        public string? avatar { get; set; }
        public string type { get; set; }
        public bool verified { get; set; }
        public Member[] members { get; set; }
    }

    public class Member
    {
        public string id { get; set; }
        public string guid { get; set; }
        public string nickname { get; set; }
    }
}
