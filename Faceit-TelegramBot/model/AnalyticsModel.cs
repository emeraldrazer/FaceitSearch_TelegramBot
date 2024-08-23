using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Faceit_TelegramBot.model
{
    public class AnalyticsModel
    {
        public long? time { get; set; }
        public long? telegram_id { get; set; }
        public string? telegram_username { get; set; }
        public string? query { get; set; }
    }
}
