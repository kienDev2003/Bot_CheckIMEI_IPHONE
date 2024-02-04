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

            if (message.StartsWith("/check "))
            {
                string imei = message.Substring("/check ".Length);
                botClient.SendTextMessageAsync(chatID, "Đang truy vấn. Vui lòng chờ !");
                if (process == 0)
                {
                    string data = CheckInfoImei(imei);
                    botClient.SendTextMessageAsync(chatID, data);
                }
                else
                {
                    botClient.SendTextMessageAsync(chatID, "Hệ thống bận. Thử lại sau 3 giây !");
                }
            }
        }

        private string CheckInfoImei(string imei)
        {
            bool start = true;
            IWebDriver chrome = null;
            do
            {
                try
                {
                    process = 1;

                    string url = ConfigurationManager.AppSettings["url_Check"].ToString();

                    ////List<ProxyInfo> proxyInfos = GetIPAndPort();
                    //string ip = "202.131.65.110";//proxyInfos[0].IP.ToString();
                    //string port = "80";//proxyInfos[0].Port.ToString();

                    ChromeDriverService chromeService = ChromeDriverService.CreateDefaultService();
                    chromeService.HideCommandPromptWindow = true;

                    ChromeOptions chromeOptions = new ChromeOptions();
                    chromeOptions.AddArgument("Mozilla / 5.0(Windows NT 10.0; Win64; x64) AppleWebKit / 537.36(KHTML, like Gecko) Chrome / 120.0.0.0 Safari / 537.36");
                    chromeOptions.AddArgument("disable-blink-features=AutomationControlled");
                    chromeOptions.AddArgument("--disable-javascript");
                    //chromeOptions.AddArgument($"--proxy-server=https://{ip}:{port}");
                    //chromeOptions.AddArgument("--headless");

                    chrome = new ChromeDriver(chromeService, chromeOptions);

                    bool goUrl = true;
                    do
                    {
                        try
                        {
                            //proxyInfos = GetIPAndPort();
                            //ip = proxyInfos[0].IP.ToString();
                            //port = proxyInfos[0].Port.ToString();

                            chrome.Navigate().GoToUrl(url);
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
                    select.SelectByValue("30");
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
                    if(chrome != null) chrome.Quit();
                    start = true;
                }
            } while (start);

            return data;
        }

        class ProxyInfo
        {
            public string IP { get; set; }

            public string Port { get; set; }
        }

        private List<ProxyInfo> GetIPAndPort()
        {
            List<ProxyInfo> list = new List<ProxyInfo>();
            string url_Proxy = ConfigurationManager.AppSettings["url_Proxy"].ToString();

            ChromeDriverService chromeService = ChromeDriverService.CreateDefaultService();
            chromeService.HideCommandPromptWindow = true;

            ChromeOptions chromeOptions = new ChromeOptions();
            chromeOptions.AddArgument("--headless");

            IWebDriver chrome = new ChromeDriver(chromeService, chromeOptions);

            chrome.Navigate().GoToUrl(url_Proxy);

            string html_Proxy = chrome.PageSource;

            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html_Proxy);

            var table = doc.DocumentNode.Descendants("table").Where(_table => _table.Attributes.Contains("class") && _table.Attributes["class"].Value == "table table-striped table-bordered").FirstOrDefault();
            var tbody = table.Descendants("tbody").FirstOrDefault();
            var tr = tbody.Descendants("tr").ToList();
            int ramdom = new Random().Next(1,19);
            var td = tr[ramdom].Descendants("td").ToList();

            list.Add(new ProxyInfo { IP = td.ElementAt(0).InnerText, Port = td.ElementAt(1).InnerText });

            chrome.Quit();

            return list;
        }
    }
}
