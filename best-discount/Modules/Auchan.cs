using AngleSharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace best_discount.Modules
{
    internal class Auchan
    {
        public static List<string> urls = new List<string> {
        "https://www.auchan.ro/ofertele-saptamanii/igiena-orala", "https://www.auchan.ro/ofertele-saptamanii/lotiuni-si-igiena-intima",
        "https://www.auchan.ro/ofertele-saptamanii/detergent-si-balsam-rufe", "https://www.auchan.ro/ofertele-saptamanii/produse-curatenie",
        "https://www.auchan.ro/ofertele-saptamanii/hartie-igienica-si-servetele", "https://www.auchan.ro/ofertele-saptamanii/produse-coafat",
        "https://www.auchan.ro/ofertele-saptamanii/scutece-si-hrana-bebe", "https://www.auchan.ro/ofertele-saptamanii/pet-food",
        "https://www.auchan.ro/ofertele-saptamanii/vase-de-gatit", "https://www.auchan.ro/ofertele-saptamanii/auto",
        "https://www.auchan.ro/ofertele-saptamanii/deodorante", "https://www.auchan.ro/ofertele-saptamanii/bauturi-alcoolice",
        "https://www.auchan.ro/ofertele-saptamanii/bauturi-racoritoare", "https://www.auchan.ro/ofertele-saptamanii/alimentatie-speciala",
        "https://www.auchan.ro/ofertele-saptamanii/alimente", "https://www.auchan.ro/ofertele-saptamanii/lactate-si-mezeluri",
        "https://www.auchan.ro/ofertele-saptamanii/dulciuri", "https://www.auchan.ro/ofertele-saptamanii/gustari-sarate",
        "https://www.auchan.ro/ofertele-saptamanii/sampon-si-gel-de-dus", "https://www.auchan.ro/ofertele-saptamanii/cafea-si-ceai"};
        public static async Task<Dictionary<string, List<Product>>> ScrapeAsync()
        {
            Console.WriteLine("Scraping Auchan...");
            var pageData = new Dictionary<string, List<Product>>();

            var config = Configuration.Default.WithDefaultLoader().WithXPath();
            var context = BrowsingContext.New(config);

            foreach (var url in urls)
            {
                var products = await ProcessPageAsync(url, context);
                foreach (var product in products)
                {
                    if (product != null && !string.IsNullOrEmpty(product.Category))
                    {
                        if (!pageData.ContainsKey(product.Category))
                        {
                            pageData[product.Category] = new List<Product>();
                        }
                        pageData[product.Category].Add(product);
                    }
                }
            }
            return pageData;
        }

        static async Task<List<Product>> ProcessPageAsync(string url, IBrowsingContext context)
        {
            var document = await context.OpenAsync(url);
            var category = document?.QuerySelector("*[xpath>'/html/body/div[2]/div/div[1]/div/div[3]/div/div[1]/div/section/div/div/div/div/h1']")?.TextContent.Trim();
            var products = new List<Product>();

            var campaignGrid = document.QuerySelector("div.vtex-search-result-3-x-gallery.flex.flex-row.flex-wrap.items-stretch.bn.ph1.na4.pl9-l");
            if (campaignGrid != null)
            {
                var items = campaignGrid.QuerySelectorAll("div.vtex-search-result-3-x-galleryItem.vtex-search-result-3-x-galleryItem--normal.pa4");
                foreach (var item in items)
                {
                    var product = new Product();
                    product.Category = category;

                    var aElement = item.QuerySelector("a");
                    var productUrl = aElement?.GetAttribute("href");

                    var imageElement = item.QuerySelector("img");
                    var image = imageElement?.GetAttribute("src");

                    var nameElement = item.QuerySelector("span.vtex-product-summary-2-x-productBrand");
                    var name = nameElement?.TextContent.Trim();

                    var priceElement = item.QuerySelector("span.vtex-product-price-1-x-sellingPrice span.vtex-product-price-1-x-currencyContainer");
                    var priceInteger = priceElement?.QuerySelector("span.vtex-product-price-1-x-currencyInteger")?.TextContent;
                    var priceFraction = priceElement?.QuerySelector("span.vtex-product-price-1-x-currencyFraction")?.TextContent;
                    var currentPrice = $"{priceInteger},{priceFraction}";

                    var originalPriceElement = item.QuerySelector("span.vtex-product-price-1-x-listPrice span.vtex-product-price-1-x-currencyContainer");
                    var originalPriceInteger = originalPriceElement?.QuerySelector("span.vtex-product-price-1-x-currencyInteger")?.TextContent;
                    var originalPriceFraction = originalPriceElement?.QuerySelector("span.vtex-product-price-1-x-currencyFraction")?.TextContent;
                    var originalPrice = $"{originalPriceInteger},{originalPriceFraction}";

                    var discountElement = item.QuerySelector("span.auchan-loyalty-0-x-listDiscountPercentage");
                    var discountPercentage = discountElement?.TextContent.Trim();

                    var dateElement = item.QuerySelector("span.auchan-loyalty-0-x-cashbackValidity");
                    var availableDate = dateElement?.TextContent.Trim();

                    product.FullTitle = name;
                    product.ProductUrl = new Uri(new Uri(url), productUrl).ToString();
                    product.Image = image;
                    product.DiscountPercentage = discountPercentage;
                    product.OriginalPrice = originalPrice;
                    product.CurrentPrice = currentPrice;
                    product.AvailableDate = availableDate;
                    //Console.WriteLine($"{category}, {product.ProductUrl}, {product.Image}, {product.DiscountPercentage}, {product.OriginalPrice}, {product.CurrentPrice}, {product.AvailableDate}");
                    products.Add(product);
                }
            }
            return products;
        }
    }
}
