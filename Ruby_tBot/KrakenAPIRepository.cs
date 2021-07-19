using ExchangeSharp;
using Newtonsoft.Json.Linq;
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
            return (await api.GetMarketSymbolsAsync(true)).ToArray();
        }

        public async Task<IWebSocket> GetTradersWebSocetAsync(string marketSymbol, Telegram.Bot.Types.CallbackQuery message)
        {
			if (marketSymbol.Contains("/"))
			{
				marketSymbol = marketSymbol.Replace(@"/", string.Empty);
			}
            return await api.GetTradesWebSocketAsync(async trader =>
            {
                string msg = $"Trade :{trader.Key}\nPrice:{trader.Value.Price};\t Amount:{trader.Value.Amount};\n" +
                $"Status:{trader.Value.Timestamp}";
                await bot.SendTextMessageAsync(message.From.Id, msg);
                await Task.Delay(3000);
            },marketSymbol);
        }
		private void ParseVolumes(JToken token, object baseVolumeKey, decimal last, out decimal baseCurrencyVolume, out decimal quoteCurrencyVolume, object quoteVolumeKey = null)
		{
			if (baseVolumeKey == null)
			{
				if (quoteVolumeKey == null)
				{
					baseCurrencyVolume = quoteCurrencyVolume = 0m;
				}
				else
				{
					quoteCurrencyVolume = token[1][quoteVolumeKey].ConvertInvariant<decimal>();
					baseCurrencyVolume = (last <= 0m ? 0m : quoteCurrencyVolume / last);
				}
			}
			else
			{
				baseCurrencyVolume = (token is JObject jObj
						? jObj.SelectToken((string)baseVolumeKey)
						: token[1][baseVolumeKey]
					).ConvertInvariant<decimal>();
				if (quoteVolumeKey == null)
				{
					quoteCurrencyVolume = baseCurrencyVolume * last;
				}
				else
				{
					quoteCurrencyVolume = token[1][quoteVolumeKey].ConvertInvariant<decimal>();
				}
			}
		}
		private T ParseCandleComponent<T>(JToken token, string marketSymbol, int periodSeconds, object openKey, object highKey, object lowKey,
			object closeKey, object timestampKey, TimestampType timestampType, object baseVolumeKey, object? quoteVolumeKey = null, object? weightedAverageKey = null)
			where T : MarketCandle, new()
		{
			T candle = new T
			{
				ClosePrice = token[1][closeKey].ConvertInvariant<decimal>(),
				ExchangeName = api.Name,
				HighPrice = token[1][highKey].ConvertInvariant<decimal>(),
				LowPrice = token[1][lowKey].ConvertInvariant<decimal>(),
				Name = marketSymbol,
				OpenPrice = token[1][openKey].ConvertInvariant<decimal>(),
				PeriodSeconds = periodSeconds,
			};

			ParseVolumes(token, baseVolumeKey, candle.ClosePrice, out decimal baseVolume, out decimal convertVolume, quoteVolumeKey);
			candle.BaseCurrencyVolume = (double)baseVolume;
			candle.QuoteCurrencyVolume = (double)convertVolume;
			if (weightedAverageKey != null)
			{
				candle.WeightedAverage = token[1][weightedAverageKey].ConvertInvariant<decimal>();
			}
			candle.Timestamp = (timestampKey == null ? CryptoUtility.UtcNow : CryptoUtility.ParseTimestamp(token[1][timestampKey], timestampType));
			return candle;
		}
		private MarketCandle ParseCandleKraken(JToken token, string marketSymbol, int periodSeconds, object openKey,
			object highKey, object lowKey,
			object closeKey, object timestampKey, TimestampType timestampType, object baseVolumeKey,
			object? quoteVolumeKey = null, object? weightedAverageKey = null)
		{
			var candle = ParseCandleComponent<MarketCandle>(token, marketSymbol, periodSeconds, openKey, highKey, lowKey, closeKey, timestampKey,
				timestampType, baseVolumeKey, quoteVolumeKey, weightedAverageKey);
			return candle;
		}

		private async Task<IWebSocket> OnGetKrakenCandleWebSocketAsync(Func<KeyValuePair<string, MarketCandle>, Task> callback, params string[] marketSymbols)
		{
			return await api.ConnectWebSocketAsync(null, async (_socket, msg) =>
			{
				JToken token = JToken.Parse(msg.ToStringFromUTF8());
				await callback(new KeyValuePair<string, MarketCandle>(marketSymbols[0],
					ParseCandleKraken(token, marketSymbols[0], 1, openKey: 2, highKey: 3, lowKey: 4, closeKey: 5,
					timestampKey: 1, TimestampType.UnixMilliseconds, baseVolumeKey: 7, quoteVolumeKey: 6, weightedAverageKey: null)));
			}, connectCallback: async (_socket) =>
			{
				List<string> marketSymbolList = new List<string>();
				foreach (var item in marketSymbols)
				{
					marketSymbolList.Add(item);
				}
				await _socket.SendMessageAsync(new
				{
					@event = "subscribe",
					pair = marketSymbolList,
					subscription = new
					{
						interval = 1,
						name = "ohlc"
					}
				});
			});
		}
		public async Task<IWebSocket> GetCandlesWebSocet(string marketSymbol, Telegram.Bot.Types.CallbackQuery message)
		{
			return await OnGetKrakenCandleWebSocketAsync(async candle =>
			{
				string chat = $"Candle :{candle.Key}   {candle.Value.ExchangeName}\n" +
				$"Price: {candle.Value.ClosePrice}  BaseVolume: {candle.Value.BaseCurrencyVolume}  QuoteVolume: {candle.Value.QuoteCurrencyVolume}";
				await bot.SendTextMessageAsync(message.From.Id, chat);
			},
			marketSymbol
			);
		}
	}
}
