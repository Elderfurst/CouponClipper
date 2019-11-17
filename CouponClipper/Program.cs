using Microsoft.Extensions.Configuration;
using PuppeteerSharp;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CouponClipper
{
    public class Program
    {
        static async Task Main()
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, true)
                .AddUserSecrets("2baf020a-49c5-4957-ae39-e6e26032dd7f")
                .Build();

            var baseUrl = config["BaseUrl"];
            var username = config["Username"];
            var password = config["Password"];

            // Download Chromium if necessary
            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);

            var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = false,
            });

            using var page = await browser.NewPageAsync();

            // Set the user agent to not read as Headless
            await page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.97 Safari/537.36");

            var result = await page.GoToAsync(baseUrl + "/signin", WaitUntilNavigation.DOMContentLoaded);

            await page.TypeAsync("#SignIn-emailInput", username);
            await page.TypeAsync("#SignIn-passwordInput", password);

            await page.ClickAsync("#SignIn-submitButton");

            await page.WaitForSelectorAsync(".KrogerHeader-Logo--inner");

            await page.GoToAsync(baseUrl + "/cl/coupons", WaitUntilNavigation.DOMContentLoaded);

            while (true)
            {
                var loadCouponButtons = @"() => {
                        const selectors = Array.from(document.querySelectorAll('.CouponButton'));
                        return selectors.map( x => { return { text: x.innerText } } );
                    }";

                var coupons = await page.EvaluateFunctionAsync<List<CouponText>>(loadCouponButtons);

                if (!coupons.Any())
                {
                    break;
                }

                var addCoupons = @"() => {
                        const selectors = Array.from(document.querySelectorAll('.CouponButton'));

                        selectors.forEach(function (item) {
                            item.click();
                        });
                    }";

                await page.EvaluateFunctionAsync(addCoupons);

                // wait for 3 seconds to let the next set of coupons to load
                await page.WaitForTimeoutAsync(3000);
            }
        }
    }

    // Used for parsing
    class CouponText
    {
        public string InnerText { get; set; }
    }
}
