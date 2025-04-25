using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Telegram.Bot;

namespace TG_Weather_Bot.Bot
{
    class CurrencyInfo
    {
        public int r030 { get; set; }
        public string txt { get; set; }
        public decimal rate { get; set; }
        public string cc { get; set; }
        public string exchangedate { get; set; }    
    }
}
