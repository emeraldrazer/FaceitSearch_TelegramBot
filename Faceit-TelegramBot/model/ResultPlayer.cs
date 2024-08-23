#pragma warning disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Faceit_TelegramBot.Model
{
    public class ResultPlayer
    {
        public string id { get; set; }
        public string guid { get; set; }
        public string nickname { get; set; }
        public string status { get; set; }
        public Game[] games { get; set; }
        public string country { get; set; }
        public string avatar { get; set; }
        public bool verified { get; set; }
        public int verification_level { get; set; }
    }

    public class Game 
    {
        public string name { get; set; }
        public int skill_level { get; set; }
    }
}
