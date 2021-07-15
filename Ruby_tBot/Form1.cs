using ExchangeSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Telegram.Bot.Types.ReplyMarkups;

namespace Ruby_tBot
{
    public partial class Form1 : Form
    {
        BackgroundWorker bw;
        public Form1()
        {
            InitializeComponent();
            this.bw = new BackgroundWorker();
            this.bw.DoWork += Bw_DoWorkAsync;
        }

        async void Bw_DoWorkAsync(object sender, DoWorkEventArgs e)
        {
            var worker = sender as BackgroundWorker;
            var key = e.Argument as String; 
            try
            {
                var bot = new Telegram.Bot.TelegramBotClient(key);
                await bot.SetWebhookAsync("");
                TelegramBotRepository botRepository = new TelegramBotRepository(bot);
                int offset = 0;
                while (true)
                {
                    var updates = await bot.GetUpdatesAsync(offset);

                    foreach (var update in updates)
                    {
                        
                        var type = update.Type;
                        if (type == Telegram.Bot.Types.Enums.UpdateType.Message)
                        {
                            var message = update.Message;
                            botRepository.GetReplyMenuAsync(message);
                        }
                        else if (type == Telegram.Bot.Types.Enums.UpdateType.CallbackQuery)
                        {
                            var message = update.CallbackQuery;
                            botRepository.GetInlineMenu(message);
                        }
                        offset = update.Id + 1;
                    }
                }
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException ex)
            { 
                Console.WriteLine(ex.Message); 
            }
        }

        private void buttonGo_Click(object sender, EventArgs e)
        {
            var key = textKey.Text;
            if (this.bw.IsBusy != true && key != "")
            {
                this.bw.RunWorkerAsync(key);
            }
        }
    }
}
