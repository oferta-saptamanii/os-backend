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
    internal class Lidl
    {
        public static async Task<Dictionary<string, List<Product>>> ScrapeAsync()
        {
            Console.WriteLine("Scraping Lidl...");
            string url = "https://www.lidl.ro/";
            var pageData = new Dictionary<string, List<Product>>();
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);
                string htmlContent = await response.Content.ReadAsStringAsync();

                var config = Configuration.Default.WithDefaultLoader();
                var context = BrowsingContext.New(config);
                var document = await context.OpenAsync(req => req.Content(htmlContent));

                // Select the div that contains all the products
                var div = document.QuerySelector("div.AHeroStageGroup__Body-Current_Sales_Week.AHeroStageGroup__Body");
                if (div != null)
                {
                    // Get the title of the page
                    var pageTitle = document.QuerySelector("title")?.TextContent.Trim();

                    var titleHref = "";
                    var imgHref = "";

                    // Select all products(each <li> with the specified class is a product)
                    var listItems = div.QuerySelectorAll("li.AHeroStageItems__Item");

                    

                    foreach (var listItem in listItems)
                    {
                        // Find the <a> element within each <li>
                        var linkElement = listItem.QuerySelector("a.AHeroStageItems__Item--Wrapper");
                        if (linkElement != null)
                        {
                            var classImg = linkElement.QuerySelector("div.TheImage.TheImage--object-fit-cover.AHeroStageItems__Item--Image");
                            if (classImg != null)
                            {
                                var imgElement = classImg.QuerySelector("img");
                                if (imgElement != null)
                                {
                                    var img = new Uri(new Uri(url), imgElement.GetAttribute("src")).ToString();
                                    var title = imgElement.GetAttribute("alt");
                                    imgHref = img;
                                    titleHref = title;
                                }
                            }

                            var href = linkElement.GetAttribute("href");
                            if (!string.IsNullOrEmpty(href))
                            {
                                var absoluteUrl = new Uri(new Uri(url), href).ToString();
                                //Console.WriteLine($"Navigating to: {absoluteUrl}");

                                var products = await ProcessPageAsync(absoluteUrl, context, titleHref, imgHref);
                                pageData.Add(titleHref, products);
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

        static async Task<List<Product>> ProcessPageAsync(string url, IBrowsingContext context, string pageTitle, string pageImg)
        {
            var document = await context.OpenAsync(url);

            var productList = new List<Product>();

            var campaignGrid = document.QuerySelector("ol.ACampaignGrid");
            if (campaignGrid != null)
            {
                var productItems = campaignGrid.QuerySelectorAll("li.ACampaignGrid__item--product.ACampaignGrid__item");
                foreach (var productItem in productItems)
                {
                    var productGridBox = productItem.QuerySelector("div.AProductGridBox");
                    if (productGridBox != null)
                    {
                        var product = new Product
                        {
                            Image = productGridBox.GetAttribute("image"),
                            FullTitle = productGridBox.GetAttribute("fulltitle"),
                            Category = productGridBox.GetAttribute("category"),
                            ProductUrl = "https://www.lidl.ro" + productGridBox.GetAttribute("canonicalurl"),
                        };

                        var detailGrids = productGridBox.QuerySelector("div.detail__grids");
                        if (detailGrids != null)
                        {
                            var dataGridData = detailGrids.GetAttribute("data-grid-data");
                            if (dataGridData != null)
                            {
                                var data = JsonConvert.DeserializeObject<List<GridData>>(System.Net.WebUtility.HtmlDecode(dataGridData));
                                if (data != null && data.Count > 0)
                                {
                                    var gridData = data[0];

                                    product.AvailableDate = gridData.Ribbons?.FirstOrDefault()?.Text;
                                    product.DiscountPercentage = gridData.Price?.Discount?.DiscountText;
                                    product.OriginalPrice = gridData.Price?.OldPrice?.ToString();
                                    product.CurrentPrice = gridData.Price?.Priced?.ToString();
                                    product.ProductImg = pageImg;
                                }
                            }
                        }

                        productList.Add(product);
                    }
                }
            }
            return productList;
        }
    }

    

    public class GridData
    {
        [JsonProperty("ribbons")]
        public List<Ribbon> Ribbons { get; set; }

        [JsonProperty("price")]
        public Price Price { get; set; }
    }

    public class Ribbon
    {
        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public class Price
    {
        [JsonProperty("price")]
        public decimal? Priced { get; set; }

        [JsonProperty("oldPrice")]
        public decimal? OldPrice { get; set; }

        [JsonProperty("discount")]
        public Discount Discount { get; set; }
    }

    public class Discount
    {
        [JsonProperty("discountText")]
        public string DiscountText { get; set; }
    }
}
