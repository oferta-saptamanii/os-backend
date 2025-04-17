using AngleSharp;
using AngleSharp.Html.Dom;
using best_discount.Models;
using best_discount.Utilities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static best_discount.Utilities.Utils;

namespace best_discount.Modules
{
    internal class Penny
    {
        public static async Task<Dictionary<string, List<Product>>> ScrapeAsync()
        {
            Console.WriteLine("Scraping Penny...");

            string url = "https://www.penny.ro/oferta-saptamanii";
            var pageData = new Dictionary<string, List<Product>>();
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);
                string htmlContent = await response.Content.ReadAsStringAsync();

                var config = Configuration.Default.WithDefaultLoader().WithXPath();
                var context = BrowsingContext.New(config);
                var document = await context.OpenAsync(req => req.Content(htmlContent));
                var div = document.QuerySelector("a.ws-show-more-tile.ws-card.fill-height");
                if (div != null)
                {
                    var href = div.GetAttribute("href");
                    var absoluteUrl = new Uri(new Uri(url), href).ToString();

                    string[] split = absoluteUrl.Split(@"/");
                    
                    string apiUrl = $@"https://www.penny.ro/api/categories/{split.Last()}/products?page=0&pageSize=3000&sortBy=relevance";


                    response = await client.GetAsync(apiUrl);
                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    JObject jsonDoc = JObject.Parse(jsonResponse);

                    var results = jsonDoc["results"];
                    foreach (var result in results)
                    {
                        var product = ProcessProduct(result);
                        if (product != null)
                        {
                            if (!pageData.ContainsKey(product.Category))
                            {
                                pageData[product.Category] = new List<Product>();
                            }

                            // Prevent duplicates
                            HashSet<Product> uniqueProducts = new HashSet<Product>(pageData[product.Category]);

                            if (!uniqueProducts.Contains(product))
                            {
                                uniqueProducts.Add(product);
                                pageData[product.Category] = uniqueProducts.ToList();
                            }
                        }
                    }

                }
                else
                {
                    Utils.Report("div not found", ErrorType.ERROR);
                }
            }
            return pageData;
        }

        private static Product ProcessProduct(JToken productToken)
        {
            var product = new Product();

            product.Category = productToken["category"]?.ToString();
            product.FullTitle = productToken["name"]?.ToString();
            product.ProductUrl = "https://www.penny.ro/products/" + productToken["slug"]?.ToString();
            product.Quantity = productToken["amount"]?.ToString() + productToken["volumeLabelShort"]?.ToString();

            var images = productToken["images"];
            if (images != null && images.Any())
            {
                product.Image = images[0]?.ToString();
            }

            var priceToken = productToken["price"];
            if (priceToken != null)
            {
                var regularToken = priceToken["regular"];
                if (regularToken != null)
                {
                    product.CurrentPrice = ConvertToCurrency(regularToken["value"]?.ToString());
                }

                

                // most likely are bad 
                product.DiscountPercentage = priceToken["discountPercentage"]?.ToString() ?? null;
                if (product.DiscountPercentage != null)
                {
                    product.OriginalPrice = ConvertToCurrency(priceToken["lowestPrice"]?.ToString().Replace(",", "."));
                    product.DiscountPercentage += "%";
                }
                else
                {
                    product.OriginalPrice = priceToken["value"]?.ToString() ?? null;
                    product.CurrentPrice = ConvertToCurrency(priceToken["lowestPrice"]?.ToString().Replace(",", "."));
                }
                    


                string validityStart = priceToken["validityStart"]?.ToString();
                string validityEnd = priceToken["validityEnd"]?.ToString();
                if(validityStart != null && validityEnd != null)
                {
                    string[] startSplit = validityStart.Split("-");
                    validityStart = $"{startSplit[2]}.{startSplit[1]}";

                    string[] endSplit = validityEnd.Split("-");
                    validityEnd = $"{endSplit[2]}.{endSplit[1]}";
                }
                if (!string.IsNullOrEmpty(validityStart) && !string.IsNullOrEmpty(validityEnd))
                {
                    product.AvailableDate = $"{validityStart} - {validityEnd}";
                }
                product.StoreName = "Penny";
            }

            return product;
        }
    }
}
