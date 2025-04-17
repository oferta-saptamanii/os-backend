using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using best_discount.Utilities;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using static best_discount.Utilities.Utils;

namespace best_discount.Services
{
    public class SeleniumService : IDisposable
    {
        private readonly ChromeDriver _driver;

        public SeleniumService()
        {
            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArgument("--headless");
            chromeOptions.AddArgument("--disable-gpu");
            chromeOptions.AddArgument("--window-size=1920,1080");
            _driver = new ChromeDriver(chromeOptions);
        }

        public ChromeDriver GetDriver()
        {
            return _driver;
        }

        public async Task ScreenshotAsync(string url, string elementSelector, string screenshotPath)
        {
            try
            {
                _driver.Navigate().GoToUrl(url);
                await Task.Delay(2000);

                var element = _driver.FindElement(By.CssSelector(elementSelector));

                Screenshot screenshot = ((ITakesScreenshot)element).GetScreenshot();
                screenshot.SaveAsFile(screenshotPath);

                Console.WriteLine($"Screenshot saved to {screenshotPath}");
            }
            catch (Exception ex)
            {
                Utils.Report($"An error occurred while taking screenshot: {ex.Message}", ErrorType.EXCEPTION);
            }
            finally
            {
                _driver.Quit();
            }
        }

        public async Task<List<(string Url, string ResourceType)>> CaptureNetworkRequests(string url)
        {
            var networkLogs = new ConcurrentBag<string>();
            var capturedUrls = new ConcurrentBag<(string Url, string ResourceType)>();

            var interceptor = _driver.Manage().Network;
            interceptor.NetworkRequestSent += (sender, e) => OnNetworkRequestSent(e, networkLogs);
            interceptor.NetworkResponseReceived += (sender, e) => OnNetworkResponseReceived(e, capturedUrls);
            await interceptor.StartMonitoring();

            Console.WriteLine(url);
            _driver.Navigate().GoToUrl(url);
            await Task.Delay(6666);

            await interceptor.StopMonitoring();

            return capturedUrls.ToList();
        }

        private void OnNetworkRequestSent(NetworkRequestSentEventArgs e, ConcurrentBag<string> networkLogs)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("Request {0}", e.RequestId).AppendLine();
            builder.AppendLine("--------------------------------");
            builder.AppendFormat("{0} {1}", e.RequestMethod, e.RequestUrl).AppendLine();
            foreach (var header in e.RequestHeaders)
            {
                builder.AppendFormat("{0}: {1}", header.Key, header.Value).AppendLine();
            }
            builder.AppendLine("--------------------------------");
            builder.AppendLine();
            networkLogs.Add(builder.ToString());
        }

        private void OnNetworkResponseReceived(NetworkResponseReceivedEventArgs e, ConcurrentBag<(string Url, string ResourceType)> capturedUrls)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("Response {0}", e.RequestId).AppendLine();
            builder.AppendLine("--------------------------------");
            builder.AppendFormat("{0} {1}", e.ResponseStatusCode, e.ResponseUrl).AppendLine();
            foreach (var header in e.ResponseHeaders)
            {
                builder.AppendFormat("{0}: {1}", header.Key, header.Value).AppendLine();
            }

            builder.AppendLine("--------------------------------");
            //Console.WriteLine($"{e.ResponseResourceType}: {e.ResponseUrl}");
            
            capturedUrls.Add((e.ResponseUrl, e.ResponseResourceType));
        }
        public void Quit()
        {
            Console.WriteLine("WebDriver closed");
            _driver.Quit();
        }

        public void Dispose()
        {
            _driver?.Dispose();
        }
    }
}
