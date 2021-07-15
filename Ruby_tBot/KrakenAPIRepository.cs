using ExchangeSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ruby_tBot
{
    class KrakenAPIRepository
    {
        ExchangeAPI api; 
        Telegram.Bot.TelegramBotClient bot;

        public KrakenAPIRepository(Telegram.Bot.TelegramBotClient _bot)
        {
            api = ExchangeAPI.GetExchangeAPI<ExchangeKrakenAPI>() as ExchangeKrakenAPI;
            bot = _bot;
        }
        public async Task<string[]> GetMarketSymbolsAsync()
        {
            return (await api.GetMarketSymbolsAsync()).ToArray();
        }

        public async Task<IWebSocket> GetTradersWebSocetAsync(string marketSymbol, Telegram.Bot.Types.CallbackQuery message)
        {
            return await api.GetTradesWebSocketAsync(async trader =>
            {
                string msg = $"Trade :{trader.Key}\nPrice:{trader.Value.Price};\t Amount:{trader.Value.Amount};\n" +
                $"Status:{trader.Value.Timestamp}";
                await bot.SendTextMessageAsync(message.From.Id, msg);
                await Task.Delay(3000);
            },marketSymbol);
        }
    }
}
