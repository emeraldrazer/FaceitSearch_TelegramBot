#pragma warning disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Faceit_TelegramBot.Model
{
    public class ResultOrg
    {
        public string guid { get; set; }
        public string name { get; set; }
        public bool partner { get; set; }
        public string avatar { get; set; }
        public bool active { get; set; }
        public string[]? games { get; set; } = null;
        public string[]? countries { get; set; } = null;
        public string[]? regions { get; set; } = null;
    }
}
