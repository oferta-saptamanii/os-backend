﻿using AngleSharp;
using best_discount.Models;
using best_discount.Services;
using best_discount.Utilities;
using System.Text.RegularExpressions;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using static best_discount.Utilities.Utils;
using SixLabors.ImageSharp.Formats.Png;
using System.Net;

namespace best_discount.Modules
{
    internal class Profi
    {
        private readonly SeleniumService _seleniumService;

        static Dictionary<string, string> profiCategories = new Dictionary<string, string>()
        {
            { "Profi Super", "64" },
            { "Profi City", "3" },
            { "Profi Go", "8" },
            { "Profi Mini", "1630" },
            { "Profi Loco", "5" },
            { "Profi", "7" },
           // { "Profi city2", "2" },
           // { "Profi loco2", "4" } ???????????? 2 duplicates
        };

        static Dictionary<string, string> catalogsUrls = new Dictionary<string, string>()
        {
            { "Profi Super", "https://www.profi.ro/revista/revista-profi-super/" },
            { "Profi City" , "https://www.profi.ro/revista/revista-profi-city/"}
        };

        public Profi(SeleniumService seleniumService)
        {
            _seleniumService = seleniumService;
        }

        public static async Task<Dictionary<string, List<Product>>> ScrapeAsync()
        {
            Console.WriteLine("Scraping Profi...");
            var pageData = new Dictionary<string, List<Product>>();

            using (HttpClient client = new HttpClient())
            {

                HttpResponseMessage response = await client.GetAsync("https://www.profi.ro");
                string htmlContent = await response.Content.ReadAsStringAsync();

                var config = Configuration.Default.WithDefaultLoader().WithXPath();
                var context = BrowsingContext.New(config);
                var document = await context.OpenAsync(req => req.Content(htmlContent));

                var url = "https://www.profi.ro/wp-admin/admin-ajax.php";

                string? availableDate;
                var availableDateEl = document.QuerySelector("*[xpath>'/html/body/div[2]/main/div[1]/div[1]/div/div[1]']");
                availableDate = (availableDateEl != null) ? availableDateEl.TextContent.Trim() : null;


                client.DefaultRequestHeaders.Add("Host", "www.profi.ro");
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:127.0) Gecko/20100101 Firefox/127.0");
                client.DefaultRequestHeaders.Add("Referer", "https://www.profi.ro/");
                client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
                client.DefaultRequestHeaders.Add("Origin", "https://www.profi.ro");
                client.DefaultRequestHeaders.Add("Connection", "keep-alive");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "empty");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
                client.DefaultRequestHeaders.Add("Priority", "u=1");
                client.DefaultRequestHeaders.Add("TE", "trailers");


                // Oferte Speciale
                // action - load_coupons_by_type
                // nonce - 65abfa63aa

                // Top oferte
                // action - load_offers_by_type
                // nonce - 43a9dd0473

                var categoryActions = new Dictionary<string, List<(string action, string nonce)>>
                {
                    {
                        "Oferte Speciale", new List<(string action, string nonce)>
                        {
                            ("load_coupons_by_type", "017de734f7")
                        }
                    },
                    {
                        "Top Oferte", new List<(string action, string nonce)>
                        {
                            ("load_offers_by_type", "849740d550")
                        }
                    }
                };

                foreach (var categoryEntry in categoryActions)
                {
                    var categoryName = categoryEntry.Key;
                    var actionsAndNonces = categoryEntry.Value;

                    var categoryProducts = new List<Product>();

                    foreach (var (action, nonce) in actionsAndNonces)
                    {
                        var uniqueProducts = new HashSet<Product>();

                        foreach (var profiCategory in profiCategories)
                        {
                            var storeType = profiCategory.Value;

                            var bodyContent = new Dictionary<string, string>
                            {
                                { "data", "{\"store_type\":" + storeType + "}" },
                                { "action", action },
                                { "nonce", nonce }
                            };

                            var encodedFormData = new FormUrlEncodedContent(bodyContent);
                            response = await client.PostAsync(url, encodedFormData);

                            if (response.IsSuccessStatusCode)
                            {
                                string responseContent = await response.Content.ReadAsStringAsync();
                                var products = ProcessPage(responseContent, profiCategory.Key, availableDate);

                                foreach (var product in products)
                                {
                                    // Prevent duplicates
                                    if (!uniqueProducts.Contains(product))
                                    {
                                        uniqueProducts.Add(product);
                                        categoryProducts.Add(product);
                                    }
                                }
                            }
                            else
                            {
                                Utils.Report($"failed scraping Profi, status code {response.StatusCode}", Utils.ErrorType.ERROR);
                            }
                        }
                    }

                    pageData.Add(categoryName, categoryProducts);
                }

            }

            // Remove duplicates
            foreach (var catName in pageData.Keys.ToList())
            {
                var catProducts = pageData[catName];
                var distProducts = catProducts.Distinct().ToList();
                pageData[catName] = distProducts;
            }

            return pageData;
        }
        private static List<Product> ProcessPage(string responseContent, string category, string date)
        {
            var products = new List<Product>();

            var productPattern = @"<div\s+class\s*=\s*['""]list-item-coupon coupon['""][^>]*?>(.*?)<\/div>\s*<\/div>\s*<\/div>\s*<\/div>";
            var productPatternTop = @"<div\s+class\s*=\s*['""]list-item-offer offer col-md-3\b[^>]*>(.*?)<\/div>\s*<\/div>\s*<\/div>\s*<\/div>";

            var imagePattern = @"<img\s+[^>]*?src\s*=\s*['""]((?:(?!stoc-limitat)[^'""])+\.(?:png|jpg|jpeg)[^'""]*)['""]";
            var fullNamePattern = @"<h2\s+[^>]*?class\s*=\s*['""]title['""][^>]*?>\s*<a\s+[^>]*?>(.*?)<\/a>";
            var originalPricePattern = @"<div\s+[^>]*?class\s*=\s*['""]price__old['""][^>]*?>(.*?)<\/div>";
            var currentPricePattern = @"<div\s+[^>]*?class\s*=\s*['""]price__new['""][^>]*?>\s*<span\s+class\s*=\s*['""]number['""]>(\d+)<\/span>\s*<div>\s*<span\s+class\s*=\s*['""]decimals['""]>(\d+)<\/span>\s*<span\s+class\s*=\s*['""]currency['""][^>]*?>\s*lei\s*<\/span>\s*<\/div>\s*<\/div>";
            var discountPattern = @"<div\s+[^>]*?class\s*=\s*['""]discount['""][^>]*?>\s*(-?\d+%)\s";

            // Different regex for top oferte/oferte speciale
            var productMatches = Regex.Matches(responseContent, productPattern, RegexOptions.Singleline);
            if (productMatches.Count == 0)
                productMatches = Regex.Matches(responseContent, productPatternTop, RegexOptions.Singleline);


            foreach (Match productMatch in productMatches)
            {
                var product = new Product();
                var productContent = productMatch.Groups[1].Value;

                var imageMatch = Regex.Match(productContent, imagePattern, RegexOptions.Singleline);
                if (imageMatch.Success)
                    product.Image = imageMatch.Groups[1].Value;

                var fullNameMatch = Regex.Match(productContent, fullNamePattern, RegexOptions.Singleline);
                if (fullNameMatch.Success)
                {
                    product.FullTitle = WebUtility.HtmlDecode(fullNameMatch.Groups[1].Value.Trim());
                    product.Quantity = GetQuantity(fullNameMatch.Groups[1].Value.Trim());
                }
                   

                var originalPriceMatch = Regex.Match(productContent, originalPricePattern, RegexOptions.Singleline);
                if (originalPriceMatch.Success)
                    product.OriginalPrice = originalPriceMatch.Groups[1].Value.Trim().Replace("<sup>", ",").Replace("</sup>", "").Replace(",", ".");


                var currentPriceMatch = Regex.Match(productContent, currentPricePattern, RegexOptions.Singleline);
                if (currentPriceMatch.Success)
                    product.CurrentPrice = $"{currentPriceMatch.Groups[1].Value}.{currentPriceMatch.Groups[2].Value}";


                var discountMatch = Regex.Match(productContent, discountPattern, RegexOptions.Singleline);
                if (discountMatch.Success)
                    product.DiscountPercentage = discountMatch.Groups[1].Value.Trim();


                product.Category = category;

                int dashIndex = date.LastIndexOf('-');
                if (dashIndex != -1)
                {
                    string dateRangePart = date.Substring(dashIndex - 5, 12).Trim();
                    product.AvailableDate = dateRangePart.Replace("\t", "").Replace("  ", " ").Replace("-", ". - ");
                }
                product.StoreName = "Profi";

                products.Add(product);

            }


            return products;
        }

        public async Task<List<Catalog>> GetCatalog()
        {
            Console.WriteLine("Scraping Profi Catalogs...");
            var url = "https://www.profi.ro/reviste-ro/";
            //var catalogUrl = "https://revista.profi.ro/f26-city-ro/full-view.html";
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

                var storesElement = document.QuerySelector(".magazine__content .content .content-inner .inner");
                var valability = storesElement?.QuerySelector(".valability");

                foreach (var store in catalogsUrls)
                {
                    

                    var allCapturedUrls = await _seleniumService.CaptureNetworkRequests(store.Value);

                    var cloudfrontUrls = allCapturedUrls
                        .Where(urls => urls.Url.Contains("cloudfront") && urls.Url.Contains("collections") && urls.ResourceType == "Image")
                        .Distinct()
                        .ToList();


                    var tempImagePaths = new List<string>();

                    // temp imgs
                    for (int i = 0; i < cloudfrontUrls.Count; i++)
                    {
                        string imageUrl = cloudfrontUrls[i].Url;
                        string tempImagePath = $"profi-temp-image-{i + 1}.jpg";
                        await DownloadImage(imageUrl, tempImagePath);
                        tempImagePaths.Add(tempImagePath);
                    }

                    Console.WriteLine($"Avem: {tempImagePaths.Count} poze");
                    string pdfFilePath = $"{DateTime.Now.ToString("mmssfff")}-profi-catalog.pdf";
                    PdfFromImages(tempImagePaths, pdfFilePath);

                    // cleanup
                    foreach (var tempImagePath in tempImagePaths)
                    {
                        File.Delete(tempImagePath);
                    }

                    catalogData.Add(new Catalog
                    {
                        AvailableDate = valability.TextContent?.Trim(),
                        Name = store.Key,
                        Url = "change later"
                    });
                }
            }
            return catalogData;
        }
    }
}