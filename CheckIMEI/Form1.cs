using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Telegram.Bot;
using Telegram.Bot.Args;

namespace CheckIMEI
{
    public partial class Form1 : Form
    {
        TelegramBotClient botClient = null;
        int process = 0;
        string data = "Không lấy được data";

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            OpenBot();
        }

        private void OpenBot()
        {
            string tokenBot = ConfigurationManager.AppSettings["tokenBot"].ToString();
            if (botClient == null)
            {
                botClient = new TelegramBotClient(tokenBot);
            }
            botClient.StartReceiving();
            botClient.OnMessage += BotClient_OnMessage;
        }

        private void BotClient_OnMessage(object sender, MessageEventArgs e)
        {
            string chatID = e.Message.Chat.Id.ToString();
            string message = e.Message.Text;

            if (message.StartsWith("/infoBasic "))
            {
                string imei = message.Substring("/infoBasic ".Length);
                botClient.SendTextMessageAsync(chatID, "Đang truy vấn. Vui lòng chờ !");
                if (process == 0)
                {
                    string data = CheckInfoImei(imei, 30);
                    botClient.SendTextMessageAsync(chatID, data);
                }
                else
                {
                    botClient.SendTextMessageAsync(chatID, "Hệ thống bận. Thử lại sau 3 giây !");
                }
            }
            else if (message.StartsWith("/lock "))
            {
                string imei = message.Substring("/lock ".Length);
                botClient.SendTextMessageAsync(chatID, "Đang truy vấn. Vui lòng chờ !");
                if (process == 0)
                {
                    string data = CheckInfoImei(imei, 103);
                    botClient.SendTextMessageAsync(chatID, data);
                }
                else
                {
                    botClient.SendTextMessageAsync(chatID, "Hệ thống bận. Thử lại sau 3 giây !");
                }
            }
            else if (message.StartsWith("/icloud "))
            {
                string imei = message.Substring("/icloud ".Length);
                botClient.SendTextMessageAsync(chatID, "Đang truy vấn. Vui lòng chờ !");
                if (process == 0)
                {
                    string data = CheckInfoImei(imei, 3);
                    botClient.SendTextMessageAsync(chatID, data);
                }
                else
                {
                    botClient.SendTextMessageAsync(chatID, "Hệ thống bận. Thử lại sau 3 giây !");
                }
            }
        }

        private string CheckInfoImei(string imei, int mode)
        {
            bool start = true;
            IWebDriver chrome = null;
            do
            {
                try
                {
                    process = 1;

                    string url = ConfigurationManager.AppSettings["url_Check"].ToString();

                    ChromeDriverService chromeService = ChromeDriverService.CreateDefaultService();
                    chromeService.HideCommandPromptWindow = true;

                    ChromeOptions chromeOptions = new ChromeOptions();
                    chromeOptions.AddArgument("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36");
                    chromeOptions.AddArgument("disable-blink-features=AutomationControlled");
                    chromeOptions.AddArgument("--disable-javascript");

                    chrome = new ChromeDriver(chromeService, chromeOptions);

                    bool goUrl = true;
                    do
                    {
                        try
                        {
                            chrome.Navigate().GoToUrl("https://sickw.com");
                            goUrl = false;
                        }
                        catch (Exception e)
                        {
                            goUrl = true;
                        }
                    }
                    while (goUrl);
                    Thread.Sleep(1000);

                    chrome.FindElement(By.Id("imei")).SendKeys(imei);

                    IWebElement chromeElement = chrome.FindElement(By.Id("service"));
                    SelectElement select = new SelectElement(chromeElement);
                    select.SelectByValue(mode.ToString());
                    Thread.Sleep(2000);

                    chrome.FindElement(By.Id("submit")).Click();

                    bool check = true;
                    int count = 0;
                    do
                    {
                        if (count == 1000)
                        {
                            chrome.Quit();

                            process = 0;

                            return data;
                        }
                        try
                        {
                            if (chrome.FindElement(By.Id("copy")) != null)
                            {
                                check = false;
                            }
                        }
                        catch (Exception e)
                        {
                            count++;
                            check = true;
                        }

                    }
                    while (check);

                    chrome.FindElement(By.Id("copy")).Click();
                    Thread.Sleep(1000);

                    this.Invoke(new Action(() =>
                    {
                        if (Clipboard.ContainsText())
                        {
                            data = Clipboard.GetText();
                            data = data.Replace("SICKW.COM", "DONE");
                        }
                    }));

                    chrome.Quit();
                    start = false;
                    process = 0;

                }
                catch (Exception e)
                {
                    if (chrome != null) chrome.Quit();
                    start = true;
                }
            } while (start);

            return data;
        }
    }
}
