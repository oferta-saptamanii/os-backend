using AngleSharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static best_discount.Utils;

namespace best_discount.Modules
{
    internal class Kaufland
    {
        public static async Task<Dictionary<string, List<Product>>> ScrapeAsync()
        {
            Console.WriteLine("Scraping Kaufland...");
            string url = "https://www.kaufland.ro/oferte/oferte-saptamanale/saptamana-curenta.html";
            var pageData = new Dictionary<string, List<Product>>();

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);
                string htmlContent = await response.Content.ReadAsStringAsync();

                var config = Configuration.Default.WithDefaultLoader().WithXPath();
                var context = BrowsingContext.New(config);
                var document = await context.OpenAsync(req => req.Content(htmlContent));

                // Select the div that contains the main offer
                var div = document.QuerySelector("#offers-overview-1");
                if (div != null)
                {
                    var boxWithin = div.QuerySelector("ul.m-accordion__list.m-accordion__list--level-2");
                    if(boxWithin != null)
                    {
                        var listItems = boxWithin.QuerySelectorAll("li.m-accordion__item.m-accordion__item--level-2");
                        foreach (var listItem in listItems)
                        {
                            var linkElement = listItem.QuerySelector("a.m-accordion__link");
                            if(linkElement != null)
                            {
                                var category = linkElement.TextContent.Trim();
                                var href = linkElement.GetAttribute("href");
                                if (!string.IsNullOrEmpty(href))
                                {
                                    var absoluteUrl = new Uri(new Uri(url), href).ToString();
                                    //Console.WriteLine($"Navigating to: {absoluteUrl} | {category}");

                                    var products = await ProcessPageAsync(absoluteUrl, context, category);
                                    pageData.Add(category, products);
                                }
                            }
                        }
                    }
                    
                }
                else
                {
                    Console.WriteLine("Div not found.");
                }

                var divSpecial = document.QuerySelector("#offers-overview-2");
                if (divSpecial != null)
                {
                    var boxWithin = divSpecial.QuerySelector("ul.m-accordion__list.m-accordion__list--level-2");
                    if (boxWithin != null)
                    {
                        var listItems = boxWithin.QuerySelectorAll("li.m-accordion__item.m-accordion__item--level-2");
                        foreach (var listItem in listItems)
                        {
                            var linkElement = listItem.QuerySelector("a.m-accordion__link");
                            if (linkElement != null)
                            {
                                var category = linkElement.TextContent.Trim();
                                var href = linkElement.GetAttribute("href");
                                if (!string.IsNullOrEmpty(href))
                                {
                                    var absoluteUrl = new Uri(new Uri(url), href).ToString();
                                    //Console.WriteLine($"Navigating to: {absoluteUrl} | {category}");

                                    var products = await ProcessPageAsync(absoluteUrl, context, category);
                                    pageData.Add(category, products);
                                }
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


        static async Task<List<Product>> ProcessPageAsync(string url, IBrowsingContext context, string category)
        {
            var document = await context.OpenAsync(url);

            var productList = new List<Product>();

            var campaignGrid = document.QuerySelector("div.g-row.g-layout-overview-tiles.g-layout-overview-tiles--offers");
            if (campaignGrid != null)
            {
                var productItems = campaignGrid.QuerySelectorAll("div.g-col.o-overview-list__list-item");
                foreach (var productItem in productItems)
                {
                    var offerTile = productItem.QuerySelector("div.o-overview-list__item-inner div.m-offer-tile.m-offer-tile--line-through.m-offer-tile--uppercase-subtitle.m-offer-tile--mobile");
                    if (offerTile != null)
                    {
                        var dataAaComponent = offerTile.GetAttribute("data-aa-component");
                        var componentData = JsonConvert.DeserializeObject<Dictionary<string, string>>(dataAaComponent);
                        var fullTitle = componentData.GetValueOrDefault("subComponent1Name");

                        var linkElement = offerTile.QuerySelector("a.m-offer-tile__link.u-button--hover-children");
                        var productUrl = linkElement?.GetAttribute("href");

                        var imgElement = offerTile.QuerySelector("div.m-offer-tile__container div.m-offer-tile__image figure.m-figure img.a-image-responsive");
                        var imageUrl = imgElement?.GetAttribute("src");

                        if (imageUrl == null || imageUrl.StartsWith("data:"))
                        {
                            imageUrl = imgElement?.GetAttribute("data-src");
                        }

                        var availableDate = document.QuerySelector("*[xpath>'/html/body/div[2]/main/div[1]/div/div/div[3]/div/div/div/div[2]/div/h2']")?.TextContent.Trim();
                        if(availableDate != null)
                        {
                            availableDate = availableDate.Replace("Valabilitate: din ", "").Replace(" până în ", " - ").Replace(".2024", ".");
                        }

                        var priceTile = offerTile.QuerySelector("div.m-offer-tile__split div.m-offer-tile__price-tiles div.a-pricetag");
                        var discountPercentage = priceTile?.QuerySelector("div.a-pricetag__discount")?.TextContent.Trim();

                        var originalPriceElement = priceTile?.QuerySelector("div.a-pricetag__old-price span.a-pricetag__old-price.a-pricetag__line-through");
                        var originalPrice = string.IsNullOrEmpty(originalPriceElement?.TextContent.Trim()) ? null : originalPriceElement.TextContent.Trim();

                        var currentPrice = priceTile?.QuerySelector("div.a-pricetag__price-container div.a-pricetag__price")?.TextContent.Trim();

                        var product = new Product
                        {
                            FullTitle = fullTitle,
                            ProductUrl = new Uri(new Uri(url), productUrl).ToString(),
                            Image = imageUrl,
                            DiscountPercentage = discountPercentage,
                            OriginalPrice = originalPrice,
                            CurrentPrice = currentPrice,
                            Category = category,
                            AvailableDate = availableDate
                        };

                        productList.Add(product);
                    }
                }
            }
            return productList;
        }

    }
}
