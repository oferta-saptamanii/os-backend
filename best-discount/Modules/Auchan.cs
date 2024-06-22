using AngleSharp;
using AngleSharp.Dom;
using best_discount.Models;
using best_discount.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static best_discount.Utilities.Utils;
using static Google.Rpc.Context.AttributeContext.Types;

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
            var category = document?.QuerySelector(".vtex-rich-text-0-x-heading.vtex-rich-text-0-x-heading--collectionTitle")?.TextContent.Trim();
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

        #region catalogs
        public static async Task<List<Catalog>> GetCatalog()
        {
            Console.WriteLine("Scraping Auchan Catalogs...");
            string url = "https://www.auchan.ro/cataloagele-auchan";
            var catalogData = new List<Catalog>();

            using (HttpClient client = new HttpClient())
            {

                string htmlContent = await FetchContent(client, url);
                if (string.IsNullOrEmpty(htmlContent))
                {
                    Utils.Report("Failed to fetch initial page content", ErrorType.ERROR);
                    return catalogData;
                }
                

                var document = await ParseHtml(htmlContent);
                
                var layoutElement = document.QuerySelector(".vtex-list-context-0-x-list.vtex-list-context-0-x-list--modular4Banner");
                var divElements = layoutElement?.QuerySelectorAll(".vtex-list-context-0-x-item.vtex-list-context-0-x-item--modular4Banner").OfType<IElement>().ToList();

                if(divElements != null)
                    foreach (var element in divElements)
                    {
                        var catalog = await CreateCatalog(client, element);
                        if(catalog != null)
                        {
                            catalogData.Add(catalog);
                        }
                    }
                else
                    Utils.Report($"divElements not found", Utils.ErrorType.ERROR);
            }

            return catalogData;
        }

        private static async Task<Catalog> CreateCatalog(HttpClient client, IElement element)
        {
            var catalog = new Catalog();

            var innerElement = element.QuerySelector("div div");
            if (innerElement != null)
            {
                var pdfElement = innerElement.QuerySelector(".vtex-list-context-0-x-infoCardImageContainer a");
                catalog.Url = pdfElement?.GetAttribute("href")?.ToString();
                catalog.Image = pdfElement?.QuerySelector("img")?.GetAttribute("src")?.ToString();

                var nameElement = innerElement.QuerySelector(".vtex-list-context-0-x-infoCardTextContainer div.vtex-list-context-0-x-infoCardHeadline");
                catalog.Name = nameElement?.TextContent?.Trim();

                var availableDateElement = innerElement.QuerySelector(".vtex-list-context-0-x-infoCardTextContainer  div.vtex-list-context-0-x-infoCardSubhead");
                var availableDate = availableDateElement?.TextContent.Trim();
                catalog.AvailableDate = ProcessAvailableDate(availableDate);
                
            }

            return catalog;
        }

        private static string ProcessAvailableDate(string date)
        {
            if (!string.IsNullOrEmpty(date))
            {
                if (date.Contains("în perioada"))
                {
                    date = date.Split("în perioada")[1];
                }
                else if (date.Contains("valabilă până pe"))
                {
                    date = date.Split("valabilă până pe")[1];
                }
                else
                    date = null;

                
            }
            return date;
        }
        #endregion
    }
}
