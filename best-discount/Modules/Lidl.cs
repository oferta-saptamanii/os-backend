using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using best_discount.Models;
using best_discount.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static best_discount.Utilities.Utils;

namespace best_discount.Modules
{
    internal class Lidl
    {
        // Get products
        #region products
        public static async Task<Dictionary<string, List<Product>>> ScrapeAsync()
        {
            Console.WriteLine("Scraping Lidl...");
            string url = "https://www.lidl.ro/";
            var pageData = new Dictionary<string, List<Product>>();

            using (HttpClient client = new HttpClient())
            {
                string htmlContent = await FetchContent(client, url);
                if (string.IsNullOrEmpty(htmlContent))
                {
                    Utils.Report("Failed to fetch initial page content", ErrorType.ERROR);
                    return pageData;
                }

                var document = await ParseHtml(htmlContent);
                var div = document.QuerySelector("div.AHeroStageGroup__Body-Current_Sales_Week.AHeroStageGroup__Body");

                if (div != null)
                {
                    var pageTitle = document.QuerySelector("title")?.TextContent.Trim();
                    var titleHref = "";
                    var imgHref = "";

                    var listItems = div.QuerySelectorAll("li.AHeroStageItems__Item");

                    foreach (var listItem in listItems)
                    {
                        var linkElement = listItem.QuerySelector("a.AHeroStageItems__Item--Wrapper");
                        if (linkElement != null)
                        {
                            var imgElement = linkElement.QuerySelector("div.TheImage.TheImage--object-fit-cover.AHeroStageItems__Item--Image img");
                            if (imgElement != null)
                            {
                                imgHref = new Uri(new Uri(url), imgElement.GetAttribute("src")).ToString();
                                titleHref = imgElement.GetAttribute("alt");
                            }

                            var href = linkElement.GetAttribute("href");
                            if (!string.IsNullOrEmpty(href))
                            {
                                var absoluteUrl = new Uri(new Uri(url), href).ToString();
                                var products = await ProcessPageAsync(absoluteUrl, document.Context, titleHref, imgHref);
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

        private static async Task<List<Product>> ProcessPageAsync(string url, IBrowsingContext context, string pageTitle, string pageImg)
        {
            var document = await context.OpenAsync(url);
            var productList = new List<Product>();

            var campaignGrid = document.QuerySelector("ol.ACampaignGrid");
            if (campaignGrid != null)
            {
                var productItems = campaignGrid.QuerySelectorAll(".ACampaignGrid__item.ACampaignGrid__item--product");

                foreach (var productItem in productItems)
                {
                    var product = ExtractProductData(productItem, pageImg);
                    if (product != null)
                    {
                        productList.Add(product);
                    }
                }
            }

            return productList;
        }

        private static Product ExtractProductData(IElement productItem, string pageImg)
        {
            var productGridBox = productItem.QuerySelector("div.AProductGridBox");
            if (productGridBox == null)
            {
                return null;
            }

            var product = new Product
            {
                Image = productGridBox.GetAttribute("image"),
                FullTitle = productGridBox.GetAttribute("fulltitle"),
                Category = productGridBox.GetAttribute("category"),
                ProductUrl = "https://www.lidl.ro" + productGridBox.GetAttribute("canonicalurl"),
                ProductImg = pageImg
            };

            var detailGrids = productGridBox.QuerySelector("div.detail__grids");
            if (detailGrids != null)
            {
                var dataGridData = detailGrids.GetAttribute("data-grid-data");
                if (!string.IsNullOrEmpty(dataGridData))
                {
                    var data = JsonConvert.DeserializeObject<List<GridData>>(System.Net.WebUtility.HtmlDecode(dataGridData));
                    if (data != null && data.Count > 0)
                    {
                        var gridData = data[0];
                        product.AvailableDate = gridData.Ribbons?.FirstOrDefault()?.Text;
                        product.DiscountPercentage = gridData.Price?.Discount?.DiscountText;
                        product.OriginalPrice = gridData.Price?.OldPrice?.ToString();
                        product.CurrentPrice = gridData.Price?.Priced?.ToString();
                    }
                }
            }

            return product;
        }
        #endregion

        // Get catalogs
        #region catalogs
        public static async Task<List<Catalog>> GetCatalog()
        {
            Console.WriteLine("Scraping Lidl Catalogs...");
            string url = "https://www.lidl.ro/";
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
                var catalogUrl = await GetCatalogUrl(document, url);

                if (!string.IsNullOrEmpty(catalogUrl))
                {
                    htmlContent = await FetchContent(client, catalogUrl);
                    if (string.IsNullOrEmpty(htmlContent))
                    {
                        Utils.Report("Failed to fetch catalog page content", ErrorType.ERROR);
                        return catalogData;
                    }

                    document = await ParseHtml(htmlContent);
                    var flyerElements = document.QuerySelectorAll(".flyer").OfType<IElement>().ToList();

                    foreach (var element in flyerElements)
                    {
                        var catalog = await CreateCatalog(client, element);
                        if (catalog != null)
                        {
                            catalogData.Add(catalog);
                        }
                    }
                }
                else
                {
                    Utils.Report("Catalog URL not found", ErrorType.ERROR);
                }
            }

            return catalogData;
        }
        private static async Task<string> GetCatalogUrl(IDocument document, string baseUrl)
        {
            var body = document.QuerySelector("[data-ga-label='Cataloage online']");
            if (body == null)
            {
                return null;
            }
            return new Uri(new Uri(baseUrl), body.GetAttribute("href")).ToString();
        }

        private static async Task<Catalog> CreateCatalog(HttpClient client, IElement element)
        {
            var catalog = new Catalog();

            var href = element.GetAttribute("href");
            if (string.IsNullOrEmpty(href))
            {
                return null;
            }

            var urlMatch = Regex.Match(href, @"\/cataloage\/(.*?)\/ar\/");
            if (!urlMatch.Success)
            {
                return null;
            }

            var apiUrl = $"https://endpoints.leaflets.schwarz/v4/flyer?flyer_identifier={urlMatch.Groups[1].Value}&region_id=0&region_code=0";
            HttpResponseMessage response = await client.GetAsync(apiUrl);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            JObject jsonDoc = JObject.Parse(await response.Content.ReadAsStringAsync());
            catalog.Url = jsonDoc["flyer"]?["pdfUrl"]?.ToString();

            var imageElement = element.QuerySelector(".flyer__image");
            catalog.Image = imageElement?.GetAttribute("src");

            var nameElement = element.QuerySelector(".flyer__name");
            var titleElement = element.QuerySelector(".flyer__title");

            if (nameElement != null && titleElement != null)
            {
                var name = nameElement.TextContent.Trim();
                var title = titleElement.TextContent.Trim();
                catalog.Name = $"{name} {title}";
                catalog.AvailableDate = title.Replace("pentru perioada", "").Trim();
            }

            return catalog;
        }
        #endregion

        

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
