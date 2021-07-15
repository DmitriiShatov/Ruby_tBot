using ExchangeSharp;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ruby_tBot
{
    class BinanceAPIRepository
    {
        static ExchangeBinanceAPI api;
        Telegram.Bot.TelegramBotClient _bot;
        
        public BinanceAPIRepository(Telegram.Bot.TelegramBotClient bot)
        {
            api = new ExchangeBinanceAPI();
            _bot = bot;
        }

        public async Task<IWebSocket> GetTradersWebSocetAsync(string marketSymbol,Telegram.Bot.Types.CallbackQuery message)
        {
            return await api.GetTradesWebSocketAsync(async trader =>
            {
                string msg = $"Trade :{trader.Key}\nPrice:{trader.Value.Price};\t Amount:{trader.Value.Amount};\n" +
                $"Status:{trader.Value.Timestamp}";
                await _bot.SendTextMessageAsync(message.From.Id, msg);
                await Task.Delay(3000);
            },
            marketSymbol
            );
        }

        public async Task<string[]> GetMarketSymbolsAsync()
        {
            return (await api.GetMarketSymbolsAsync()).ToArray();
        }
        private static T ParseCandleComponentBinance<T>(JToken token, string marketSymbol, int periodSeconds, object openKey, object highKey, object lowKey,
            object closeKey, object timestampKey, TimestampType timestampType, object baseVolumeKey, object? quoteVolumeKey = null, object? weightedAverageKey = null)
            where T : MarketCandle, new()
        {
            T candle = new T
            {
                ClosePrice = token["k"][closeKey].ConvertInvariant<decimal>(),
                ExchangeName = api.Name,
                HighPrice = token["k"][highKey].ConvertInvariant<decimal>(),
                LowPrice = token["k"][lowKey].ConvertInvariant<decimal>(),
                Name = marketSymbol,
                OpenPrice = token["k"][openKey].ConvertInvariant<decimal>(),
                PeriodSeconds = periodSeconds,
            };

            ParseVolumes(token, baseVolumeKey, candle.ClosePrice, out decimal baseVolume, out decimal convertVolume, quoteVolumeKey);
            candle.BaseCurrencyVolume = (double)baseVolume;
            candle.QuoteCurrencyVolume = (double)convertVolume;
            if (weightedAverageKey != null)
            {
                candle.WeightedAverage = token["k"][weightedAverageKey].ConvertInvariant<decimal>();
            }
            candle.Timestamp = (timestampKey == null ? CryptoUtility.UtcNow : CryptoUtility.ParseTimestamp(token["k"][timestampKey], timestampType));
            return candle;
        }
        private static MarketCandle ParseCandleBinance(JToken token, string marketSymbol, int periodSeconds, object openKey,
            object highKey, object lowKey,
            object closeKey, object timestampKey, TimestampType timestampType, object baseVolumeKey,
            object? quoteVolumeKey = null, object? weightedAverageKey = null)
        {
            var candle = ParseCandleComponentBinance<MarketCandle>(token, marketSymbol, periodSeconds, openKey, highKey, lowKey, closeKey, timestampKey,
                timestampType, baseVolumeKey, quoteVolumeKey, weightedAverageKey);
            return candle;
        }
        public static void ParseVolumes(JToken token, object baseVolumeKey, decimal last, out decimal baseCurrencyVolume, out decimal quoteCurrencyVolume, object quoteVolumeKey = null)
        {
            if (baseVolumeKey == null)
            {
                if (quoteVolumeKey == null)
                {
                    baseCurrencyVolume = quoteCurrencyVolume = 0m;
                }
                else
                {
                    quoteCurrencyVolume = token["k"][quoteVolumeKey].ConvertInvariant<decimal>();
                    baseCurrencyVolume = (last <= 0m ? 0m : quoteCurrencyVolume / last);
                }
            }
            else
            {
                baseCurrencyVolume =  token["k"][baseVolumeKey].ConvertInvariant<decimal>();
                if (quoteVolumeKey == null)
                {
                    quoteCurrencyVolume = baseCurrencyVolume * last;
                }
                else
                {
                    quoteCurrencyVolume = token["k"][quoteVolumeKey].ConvertInvariant<decimal>();
                }
            }
        }
        private async Task<IWebSocket> OnGetBinanceCandlesWebSocketAsync(Func<KeyValuePair<string, MarketCandle>, Task> callback, params string[] marketSymbols)
        {
            string url = await GetWebSocketStreamUrlForSymbolsAsync("@kline_1m", marketSymbols);
            return await api.ConnectWebSocketAsync(url, async (_socket, msg) =>
            {
                JToken token = JToken.Parse(msg.ToStringFromUTF8());
                string name = token["stream"].ToStringInvariant();
                token = token["data"];
                string marketSymbol = api.NormalizeMarketSymbol(name.Substring(0, name.IndexOf('@')));
                await callback(new KeyValuePair<string, MarketCandle>(marketSymbol,
                    ParseCandleBinance(token, marketSymbol, 5000, openKey: "o", highKey: "h", lowKey: "l", closeKey: "c",
                    timestampKey: "T", TimestampType.UnixMilliseconds, baseVolumeKey: "V", quoteVolumeKey: "Q", weightedAverageKey: "v")));
            });
        }
        private async Task<string> GetWebSocketStreamUrlForSymbolsAsync(string suffix, params string[] marketSymbols)
        {
            if (marketSymbols == null || marketSymbols.Length == 0)
            {
                marketSymbols = (await api.GetMarketSymbolsAsync()) as string[];
            }

            StringBuilder streams = new StringBuilder("/stream?streams=");
            for (int i = 0; i < marketSymbols.Length; i++)
            {
                string marketSymbol = api.NormalizeMarketSymbol(marketSymbols[i]).ToLowerInvariant();
                streams.Append(marketSymbol);
                streams.Append(suffix);
                streams.Append('/');
            }
            streams.Length--;

            return streams.ToString();
        }
        public async Task<IWebSocket> GetCandlesWebSocet(string marketSymbol, Telegram.Bot.Types.CallbackQuery message)
        {
            return await OnGetBinanceCandlesWebSocketAsync(async candle =>
            {
                string chat = $"Candle :{candle.Key}   {candle.Value.ExchangeName}\n" +
                $"Price: {candle.Value.ClosePrice}  BaseVolume: {candle.Value.BaseCurrencyVolume}  QuoteVolume: {candle.Value.QuoteCurrencyVolume}";
                await _bot.SendTextMessageAsync(message.From.Id, chat);
            },
            marketSymbol
            );
        }
    }
}
