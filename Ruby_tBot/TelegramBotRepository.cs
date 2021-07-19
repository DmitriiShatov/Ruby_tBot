using ExchangeSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;

namespace Ruby_tBot
{
    class TelegramBotRepository
    {
        Telegram.Bot.TelegramBotClient _bot;
        IWebSocket socket;
        BinanceAPIRepository binanceApi;
        KrakenAPIRepository krakenApi;
        int counter = 0;
        string[] arrayKeyboard;
        public string marketSymbol { get; private set; }
        public TelegramBotRepository(Telegram.Bot.TelegramBotClient bot)
        {
            _bot = bot;
        }

        private InlineKeyboardButton[][] GetKeyboardsButtons(string[] keyboard)
        {
            InlineKeyboardButton[][] kb = null;
            if (keyboard.Length > 10)
            {
                int num = 0;
                kb = new InlineKeyboardButton[keyboard.LongLength / 4][];
                for (int i = 0; i < keyboard.Length / 4; i++)
                { 
                    InlineKeyboardButton[] replyKeyboard = new InlineKeyboardButton[4];
                    for (int j = 0; j < 4; ++j)
                    {
                            var button = new InlineKeyboardButton();
                            button = keyboard[num];
                            replyKeyboard[j] = button;
                            kb[i] = replyKeyboard;
                            num++;
                    }
                    
                }
            }
            else 
            {
                kb = new InlineKeyboardButton[1][];
                var replyKeyboard = new InlineKeyboardButton[keyboard.Length];
                for (int i = 0; i < keyboard.Length; ++i)
                {
                    var button = new InlineKeyboardButton();
                    button = keyboard[i];
                    replyKeyboard[i] = button;
                }
                kb[0] = replyKeyboard;
            }
            return kb;
        }

        public async void GetReplyMenuAsync(Telegram.Bot.Types.Message message)
        {
            switch (message.Text.ToLower())
            {
                case "start":
                    var Keyboard = new ReplyKeyboardMarkup
                    {
                        Keyboard = new[]
                                        {
                                        new[] {
                                                new KeyboardButton("Select Changes"),
                                              },
                                     },
                        ResizeKeyboard = true
                    };
                    await _bot.SendTextMessageAsync(message.From.Id, "Press button Select Changes", replyMarkup: Keyboard);
                    break;
                case "select changes":
                    var keyboard = new InlineKeyboardMarkup(new InlineKeyboardButton[][]
                                        {new [] {
                                                 InlineKeyboardButton.WithCallbackData("Binance","binance"),
                                                 InlineKeyboardButton.WithCallbackData("Kraken","kraken")
                                                 }
                                       });
                    await _bot.SendTextMessageAsync(message.From.Id, "Chose a category.", replyMarkup: keyboard);
                    break;
                case "stop":
                    socket.Dispose();
                    binanceApi = null;
                    krakenApi = null;
                    arrayKeyboard = null;
                    counter = 0;
                    var StopKeyboard = new ReplyKeyboardMarkup
                    {
                        Keyboard = new[]
                        {
                                        new[] {
                                                new KeyboardButton("Start"),
                                              },
                                     },
                        ResizeKeyboard = true
                    };
                    await _bot.SendTextMessageAsync(message.From.Id, "Press Start to begin work", replyMarkup: StopKeyboard);
                    break;
                default:
                    var StartKeyboard = new ReplyKeyboardMarkup
                    {
                        Keyboard = new[]
                                                            {
                                        new[] {
                                                new KeyboardButton("Select Changes"),
                                              },
                                     },
                        ResizeKeyboard = true
                    };
                    await _bot.SendTextMessageAsync(message.From.Id, "Press Button Select Changes", replyMarkup: StartKeyboard);
                    break;
            }
        }

        public async void GetInlineMenu(Telegram.Bot.Types.CallbackQuery message)
        {
            if (binanceApi != null)
            {
                foreach (var item in binanceApi.GetMarketSymbolsAsync().Result)
                {

                    if (message.Data == item)
                    {
                        marketSymbol = item;
                        string[] arr = new string[2] { "Traders", "Candles" };
                        var keyboard = new InlineKeyboardMarkup(GetKeyboardsButtons(arr));
                        await _bot.SendTextMessageAsync(message.From.Id, "Select a Type", replyMarkup: keyboard);
                    }

                }
            }
            else if (krakenApi != null)
            {
                foreach (var item in krakenApi.GetMarketSymbolsAsync().Result)
                {

                    if (message.Data == item)
                    {
                        marketSymbol = item;
                        string[] arr = new string[2] { "Traders", "Candles" };
                        var keyboard = new InlineKeyboardMarkup(GetKeyboardsButtons(arr));
                        await _bot.SendTextMessageAsync(message.From.Id, "Select a Type", replyMarkup: keyboard);
                    }

                }
            }
            switch (message.Data.ToLower())
            {
                case "binance":
                    binanceApi = new BinanceAPIRepository(_bot);
                    arrayKeyboard = binanceApi.GetMarketSymbolsAsync().Result;
                    if (arrayKeyboard.Length <= 100)
                    {
                        var binanceKeyboard = new InlineKeyboardMarkup(GetKeyboardsButtons(arrayKeyboard));
                        await _bot.SendTextMessageAsync(message.From.Id, "Select a Global Symbol", replyMarkup: binanceKeyboard);
                    }
                    else 
                    {
                        string[] partOfSymbol = new string[100];
                        for (int i = 0; i < partOfSymbol.Length; ++i)
                        {
                            if (i == 99)
                            {
                                partOfSymbol[i] = "More...";
                                break;
                            }
                            else
                            {
                                partOfSymbol[i] = arrayKeyboard[counter];
                                counter++;
                            }
                        }
                        var keyboard = new InlineKeyboardMarkup(GetKeyboardsButtons(partOfSymbol));
                        await _bot.SendTextMessageAsync(message.From.Id, "Select a Market Symbol", replyMarkup: keyboard);
                    }
                    break;
                case "kraken":
                    krakenApi = new KrakenAPIRepository(_bot);
                    arrayKeyboard = krakenApi.GetMarketSymbolsAsync().Result;
                    if (arrayKeyboard.Length <= 100)
                    {
                        var keyboard = new InlineKeyboardMarkup(GetKeyboardsButtons(arrayKeyboard));
                        await _bot.SendTextMessageAsync(message.From.Id, "Select a Market Symbol", replyMarkup: keyboard);
                    }
                    else
                    {
                        string[] partOfSymbol = new string[100];
                        for (int i = 0; i < partOfSymbol.Length; ++i)
                        {
                            if (i == 99)
                            {
                                partOfSymbol[i] = "More...";
                                break;
                            }
                            else
                            {
                                partOfSymbol[i] = arrayKeyboard[counter];
                                counter++;
                            }
                        }
                        var keyboard = new InlineKeyboardMarkup(GetKeyboardsButtons(partOfSymbol));
                        await _bot.SendTextMessageAsync(message.From.Id,"Select a Market Symbol",replyMarkup: keyboard);
                    }
                    break;
                case "traders":
                    if (binanceApi != null)
                    {
                        socket = await binanceApi.GetTradersWebSocetAsync(marketSymbol, message);
                        var Keyboard = new ReplyKeyboardMarkup
                        {
                            Keyboard = new[]
                            {
                                        new[] {
                                                new KeyboardButton("Stop"),
                                              },
                                     },
                            ResizeKeyboard = true
                        };
                        await _bot.SendTextMessageAsync(message.From.Id, "Press \"Stop\" to Unsubscribe", replyMarkup: Keyboard);
                    }
                    else if (krakenApi != null)
                    {
                        socket = await krakenApi.GetTradersWebSocetAsync(marketSymbol, message);
                        var Keyboard = new ReplyKeyboardMarkup
                        {
                            Keyboard = new[]
                            {
                                        new[] {
                                                new KeyboardButton("Stop"),
                                              },
                                     },
                            ResizeKeyboard = true
                        };
                        await _bot.SendTextMessageAsync(message.From.Id, "Press \"Stop\" to Unsubscribe", replyMarkup: Keyboard);
                    }
                    break;
                case "candles":
                    if (binanceApi != null)
                    {
                        socket = await binanceApi.GetCandlesWebSocet(marketSymbol, message);
                        var candleKeyboard = new ReplyKeyboardMarkup
                        {
                            Keyboard = new[]
                            {
                                        new[] {
                                                new KeyboardButton("Stop"),
                                              },
                                     },
                            ResizeKeyboard = true
                        };
                        await _bot.SendTextMessageAsync(message.From.Id, "Press \"Stop\" to Unsubscribe", replyMarkup: candleKeyboard);
                    }
                    else if (krakenApi != null)
                    {
                        socket = await krakenApi.GetCandlesWebSocet(marketSymbol, message);
                        var candleKeyboard = new ReplyKeyboardMarkup
                        {
                            Keyboard = new[]
                            {
                                        new[] {
                                                new KeyboardButton("Stop"),
                                              },
                                     },
                            ResizeKeyboard = true
                        };
                        await _bot.SendTextMessageAsync(message.From.Id, "Press \"Stop\" to Unsubscribe", replyMarkup: candleKeyboard);
                    }
                    break;
                case "more...":
                    if ((arrayKeyboard.Length - counter) < 100)
                    {
                        string[] partOfSymbol = new string[arrayKeyboard.Length - counter];
                        for (int i = 0; i < partOfSymbol.Length; ++i)
                        {
                            partOfSymbol[i] = arrayKeyboard[counter];
                            counter++;
                        }
                        var Keyboard = new InlineKeyboardMarkup(GetKeyboardsButtons(partOfSymbol));
                        await _bot.SendTextMessageAsync(message.From.Id, "Select a Market Symbol", replyMarkup: Keyboard);
                        counter = 0;
                    }
                    else
                    {
                        string[] partOfSymbol = new string[100];
                        for (int i = 0; i < partOfSymbol.Length; ++i)
                        {
                            if (i == 99)
                            {
                                partOfSymbol[i] = "More...";
                                break;
                            }
                            else
                            {
                                partOfSymbol[i] = arrayKeyboard[counter];
                                counter++;
                            }
                        }
                        var Keyboard = new InlineKeyboardMarkup(GetKeyboardsButtons(partOfSymbol));
                        await _bot.SendTextMessageAsync(message.From.Id, "Select a Market Symbol", replyMarkup: Keyboard);
                    }
                    break;
            }           
        }

    }
}
