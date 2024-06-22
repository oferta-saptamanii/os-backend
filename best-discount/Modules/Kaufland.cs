using AngleSharp;
using AngleSharp.Dom;
using best_discount.Models;
using best_discount.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using static best_discount.Utilities.Utils;

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
                        if (availableDate != null)
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

        // Get catalogs
        #region catalogs

        public static async Task<Dictionary<string, Dictionary<string, List<Catalog>>>> GetAllCatalogs()
        {
            var stores = GetStores();
            var result = new Dictionary<string, Dictionary<string, List<Catalog>>>();

            Console.WriteLine($"Scraping Kaufland Catalogs...");

            foreach (var store in stores)
            {
                var storeCatalogs = await GetCatalog(store.Id);

                if (!result.ContainsKey(store.City))
                {
                    result[store.City] = new Dictionary<string, List<Catalog>>();
                }

                if (!result[store.City].ContainsKey(store.Name))
                {
                    result[store.City][store.Name] = new List<Catalog>();
                }

                result[store.City][store.Name].AddRange(storeCatalogs);
            }

            return result;
        }

        public static async Task<List<Catalog>> GetCatalog(string storeId)
        {
            string url = "https://www.kaufland.ro/cataloage-cu-reduceri.html#top";
            var catalogData = new List<Catalog>();

            using (HttpClient client = new HttpClient())
            {
                // Set store location
                client.DefaultRequestHeaders.Add("Cookie", $"x-aem-variant=RO{storeId};");

                string htmlContent = await FetchContent(client, url);
                if (string.IsNullOrEmpty(htmlContent))
                {
                    Utils.Report("Failed to fetch initial page content", ErrorType.ERROR);
                    return catalogData;
                }

                var document = await ParseHtml(htmlContent);

                var divElements = document.QuerySelectorAll("[data-t-decorator='Catalog']").OfType<IElement>().ToList();

                foreach (var element in divElements)
                {
                    var flyerElement = element.QuerySelector("a");
                    if (flyerElement != null)
                    {
                        var catalog = await CreateCatalog(client, flyerElement);
                        if (flyerElement != null)
                        {

                            catalogData.Add(catalog);
                        }
                    }
                }
            }

            return catalogData;
        }

        private static async Task<Catalog> CreateCatalog(HttpClient client, IElement element)
        {
            var catalog = new Catalog();

            var href = element.GetAttribute("href");
            if (string.IsNullOrEmpty(href))
            {
                return null;
            }

            // https://leaflets.kaufland.com/ro-RO/RO_ro_Magazine2_3970_RO24-OC2/ar/3970
            var urlMatch = Regex.Match(href, @"\/ro-RO\/(.*?)\/ar\/");
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


            string name = "";
            var teaserWrapper = element.QuerySelector(".m-teaser__wrapper");
            if (teaserWrapper != null)
            {
                var description = teaserWrapper.QuerySelector(".m-teaser__desc")?.TextContent?.Trim();
                description = description.Replace("Răsfoiește c", "C");

                catalog.Name = description;

                var divImgElement = teaserWrapper.QuerySelector(".m-teaser__image");
                if (divImgElement != null)
                {
                    var figureElement = divImgElement.QuerySelector(".m-figure");
                    if (figureElement != null)
                    {
                        var imgElement = figureElement.QuerySelector("a-image-responsive");
                        var img = imgElement?.GetAttribute("src");

                        catalog.Image = img;
                    }
                }
            }
            var imageElement = element.QuerySelector(".flyer__image");
            catalog.Image = imageElement?.GetAttribute("src");
            catalog.AvailableDate = element?.GetAttribute("data-aa-detail")?.Replace("Catalogul cu oferte valabile în perioada", "").Trim();

            return catalog;
        }

        // turn this to a json later
        public static List<Store> GetStores()
        {
            return new List<Store>
            {

                new Store { Id = "1000", Name = "Bucuresti-Colentina", City = "Bucuresti" },
                new Store { Id = "7000", Name = "Bucuresti-Brancusi", City = "Bucuresti" },
                new Store { Id = "1200", Name = "Ramnicu Valcea-Libertatii", City = "Ramnicu Valcea" },
                new Store { Id = "1100", Name = "Ploiesti-Vest", City = "Ploiesti" },
                new Store { Id = "1600", Name = "Alba Iulia-Cetate", City = "Alba Iulia" },
                new Store { Id = "1300", Name = "Timisoara-Cetate", City = "Timisoara" },
                new Store { Id = "1500", Name = "Baia Mare-Republicii-George Cosbuc", City = "Baia Mare" },
                new Store { Id = "2100", Name = "Targu Mures-Orasul de Jos", City = "Targu Mures" },
                new Store { Id = "1700", Name = "Galati-Micro 21", City = "Galati" },
                new Store { Id = "1400", Name = "Cluj Napoca-Manastur", City = "Cluj-Napoca" },
                new Store { Id = "1900", Name = "Hunedoara", City = "Hunedoara" },
                new Store { Id = "1800", Name = "Satu Mare-Careiului", City = "Satu Mare" },
                new Store { Id = "2200", Name = "Suceava-George Enescu", City = "Suceava" },
                new Store { Id = "4400", Name = "Timisoara-Elisabetin", City = "Timisoara" },
                new Store { Id = "2000", Name = "Targoviste-Centrum", City = "Targoviste" },
                new Store { Id = "4600", Name = "Bistrita-Independentei Sud", City = "Bistrita" },
                new Store { Id = "2500", Name = "Pitesti-Razboieni", City = "Pitesti" },
                new Store { Id = "2600", Name = "Bucuresti-Tei", City = "Bucuresti" },
                new Store { Id = "2400", Name = "Piatra Neamt-Obor", City = "Piatra Neamt" },
                new Store { Id = "5300", Name = "Turda", City = "Turda" },
                new Store { Id = "3000", Name = "Targu Jiu-Str. Luncilor", City = "Targu Jiu" },
                new Store { Id = "4300", Name = "Sibiu-Selimbar", City = "Sibiu" },
                new Store { Id = "3900", Name = "Iasi-Nicolina", City = "Iasi" },
                new Store { Id = "4700", Name = "Cluj Napoca-Marasti", City = "Cluj-Napoca" },
                new Store { Id = "2300", Name = "Constanta-Obor", City = "Constanta" },
                new Store { Id = "3400", Name = "Roman-Soseaua de Centura", City = "Roman" },
                new Store { Id = "3300", Name = "Brasov-Noua-Darste", City = "Brasov" },
                new Store { Id = "4000", Name = "Iasi-Pacurari", City = "Iasi" },
                new Store { Id = "3600", Name = "Arad-Centru", City = "Arad" },
                new Store { Id = "3200", Name = "Zalau-Simion Barnutiu", City = "Zalau" },
                new Store { Id = "3800", Name = "Iasi-Alexandru cel Bun", City = "Iasi" },
                new Store { Id = "5000", Name = "Odorheiu Secuiesc", City = "Odorheiu Secuiesc" },
                new Store { Id = "4900", Name = "Braila-Braila Sud", City = "Braila" },
                new Store { Id = "5700", Name = "Craiova-Craiovita", City = "Craiova" },
                new Store { Id = "2900", Name = "Slatina-Centrul Vechi", City = "Slatina" },
                new Store { Id = "3700", Name = "Craiova-Valea Rosie", City = "Craiova" },
                new Store { Id = "4800", Name = "Focsani-Brailei", City = "Focsani" },
                new Store { Id = "4200", Name = "Pitesti-Gavana", City = "Pitesti" },
                new Store { Id = "5500", Name = "Buzau-Strada Frasinet", City = "Buzau" },
                new Store { Id = "5200", Name = "Bacau-Zentrum", City = "Bacau" },
                new Store { Id = "5900", Name = "Bucuresti-Rahova", City = "Bucuresti" },
                new Store { Id = "6500", Name = "Resita-Lunca Barzavei", City = "Resita" },
                new Store { Id = "6700", Name = "Slobozia", City = "Slobozia" },
                new Store { Id = "5800", Name = "Onesti", City = "Onesti" },
                new Store { Id = "2800", Name = "Miercurea Ciuc", City = "Miercurea Ciuc" },
                new Store { Id = "8200", Name = "Baia Mare-Vasile Alecsandri", City = "Baia Mare" },
                new Store { Id = "5400", Name = "Medias-Vitrometan", City = "Medias" },
                new Store { Id = "8300", Name = "Botosani-Primaverii", City = "Botosani" },
                new Store { Id = "7700", Name = "Mioveni", City = "Mioveni" },
                new Store { Id = "7100", Name = "Reghin", City = "Reghin" },
                new Store { Id = "7900", Name = "Giurgiu", City = "Giurgiu" },
                new Store { Id = "6800", Name = "Campulung", City = "Campulung" },
                new Store { Id = "7300", Name = "Tulcea-Barajului", City = "Tulcea" },
                new Store { Id = "7200", Name = "Galati-Micro 40", City = "Galati" },
                new Store { Id = "7600", Name = "Orastie", City = "Orastie" },
                new Store { Id = "2700", Name = "Deva", City = "Deva" },
                new Store { Id = "8600", Name = "Ramnicu Sarat", City = "Ramnicu Sarat" },
                new Store { Id = "1370", Name = "Carei", City = "Carei" },
                new Store { Id = "8400", Name = "Sighetu Marmatiei", City = "Sighetu Marmatiei" },
                new Store { Id = "1470", Name = "Timisoara-Dumbravita", City = "Timisoara" },
                new Store { Id = "8900", Name = "Gheorgheni", City = "Gheorgheni" },
                new Store { Id = "6600", Name = "Sighisoara", City = "Sighisoara" },
                new Store { Id = "8800", Name = "Targu Secuiesc", City = "Targu Secuiesc" },
                new Store { Id = "7400", Name = "Vaslui", City = "Vaslui" },
                new Store { Id = "1270", Name = "Oradea-Rogerius", City = "Oradea" },
                new Store { Id = "8500", Name = "Caransebes", City = "Caransebes" },
                new Store { Id = "7500", Name = "Sebes", City = "Sebes" },
                new Store { Id = "4100", Name = "Ploiesti-Nord", City = "Ploiesti" },
                new Store { Id = "6900", Name = "Fagaras", City = "Fagaras" },
                new Store { Id = "1970", Name = "Tecuci", City = "Tecuci" },
                new Store { Id = "7800", Name = "Comanesti", City = "Comanesti" },
                new Store { Id = "8100", Name = "Campina", City = "Campina" },
                new Store { Id = "5600", Name = "Sfantu Gheorghe", City = "Sfantu Gheorghe" },
                new Store { Id = "8700", Name = "Medgidia", City = "Medgidia" },
                new Store { Id = "1170", Name = "Bistrita-Calea Moldovei", City = "Bistrita" },
                new Store { Id = "1870", Name = "Alexandria", City = "Alexandria" },
                new Store { Id = "1770", Name = "Drobeta Turnu Severin", City = "Drobeta Turnu Severin" },
                new Store { Id = "3500", Name = "Bucuresti-Vitan, Racari", City = "Bucuresti" },
                new Store { Id = "2070", Name = "Navodari", City = "Navodari" },
                new Store { Id = "2270", Name = "Iasi-Tudor Vladimirescu", City = "Iasi" },
                new Store { Id = "1670", Name = "Calarasi", City = "Calarasi" },
                new Store { Id = "1570", Name = "Curtea de Arges", City = "Curtea de Arges" },
                new Store { Id = "2170", Name = "Pascani", City = "Pascani" },
                new Store { Id = "2670", Name = "Radauti", City = "Radauti" },
                new Store { Id = "8000", Name = "Falticeni", City = "Falticeni" },
                new Store { Id = "6400", Name = "Galati-Tiglina 4", City = "Galati" },
                new Store { Id = "2370", Name = "Ploiesti-Sud", City = "Ploiesti" },
                new Store { Id = "2770", Name = "Petrosani", City = "Petrosani" },
                new Store { Id = "3370", Name = "Bucuresti-Militari", City = "Bucuresti" },
                new Store { Id = "2470", Name = "Bucuresti-Pantelimon", City = "Bucuresti" },
                new Store { Id = "3970", Name = "Targu Mures-Tudor Vladimirescu", City = "Targu Mures" },
                new Store { Id = "6000", Name = "Oradea-Nufarul", City = "Oradea" },
                new Store { Id = "3570", Name = "Targoviste-Micro VI", City = "Targoviste" },
                new Store { Id = "4270", Name = "Arad-Sega", City = "Arad" },
                new Store { Id = "2870", Name = "Bucuresti-Bucurestii Noi", City = "Bucuresti" },
                new Store { Id = "2570", Name = "Gherla", City = "Vatra Gherla" },
                new Store { Id = "4470", Name = "Vatra Dornei", City = "Vatra Dornei" },
                new Store { Id = "5100", Name = "Oradea-Iosia", City = "Oradea" },
                new Store { Id = "3100", Name = "Bucuresti-Aparatorii-Patriei", City = "Bucuresti" },
                new Store { Id = "2970", Name = "Bacau-Carpati Cornisa", City = "Bacau" },
                new Store { Id = "3070", Name = "Bucuresti-Tudor Vladimirescu", City = "Bucuresti" },
                new Store { Id = "4070", Name = "Slatina-Piata Garii", City = "Slatina" },
                new Store { Id = "4170", Name = "Constanta-Viile Noi", City = "Constanta" },
                new Store { Id = "5270", Name = "Sibiu-Strand", City = "Sibiu" },
                new Store { Id = "4500", Name = "Timisoara-Fabric", City = "Timisoara" },
                new Store { Id = "4770", Name = "Braila-1 Mai", City = "Braila" },
                new Store { Id = "5570", Name = "Arad-Micalaca", City = "Arad" },
                new Store { Id = "4370", Name = "Bucuresti-Basarab", City = "Bucuresti" },
                new Store { Id = "3170", Name = "Bucuresti-Ferentari", City = "Bucuresti" },
                new Store { Id = "4570", Name = "Mangalia-Saturn", City = "Mangalia" },
                new Store { Id = "3670", Name = "Ramnicu Valcea-Nord", City = "Ramnicu Sarat" },
                new Store { Id = "6970", Name = "Codlea", City = "Codlea" },
                new Store { Id = "5170", Name = "Lugoj", City = "Lugoj" },
                new Store { Id = "6870", Name = "Brasov-Bartolomeu", City = "Brasov" },
                new Store { Id = "5670", Name = "Craiova-1Mai", City = "Craiova" },
                new Store { Id = "4670", Name = "Cluj Napoca-Gheorgheni", City = "Cluj-Napoca" },
                new Store { Id = "6770", Name = "Satu Mare-Centru", City = "Satu Mare" },
                new Store { Id = "3870", Name = "Buzau-Unirii", City = "Buzau" },
                new Store { Id = "5470", Name = "Bucuresti-Odai", City = "Bucuresti" },
                new Store { Id = "7770", Name = "Resita-Lunca Pomostului", City = "Resita" },
                new Store { Id = "6300", Name = "Bacau-Aviatorilor", City = "Bacau" },
                new Store { Id = "8970", Name = "Bragadiru Centru", City = "Bragadiru" },
                new Store { Id = "7570", Name = "Focsani-Marasesti", City = "Focsani" },
                new Store { Id = "8870", Name = "Constanta-Maritimo", City = "Constanta" },
                new Store { Id = "7370", Name = "Buftea-Crevedia", City = "Buftea" },
                new Store { Id = "4310", Name = "Cluj Napoca-Intre Lacuri", City = "Cluj-Napoca" },
                new Store { Id = "7270", Name = "Sibiu-Centru", City = "Sibiu" },
                new Store { Id = "4510", Name = "Moreni", City = "Moreni" },
                new Store { Id = "7670", Name = "Bucuresti-Vitan, Energeticienilor", City = "Bucuresti" },
                new Store { Id = "6370", Name = "Dej", City = "Dej" },
                new Store { Id = "7470", Name = "Bucuresti-Titan, Theodor Pallady", City = "Bucuresti" },
                new Store { Id = "3110", Name = "Botosani-Soseaua Iasului", City = "Botosani" },
                new Store { Id = "8370", Name = "Bucuresti-Sisesti", City = "Bucuresti" },
                new Store { Id = "8570", Name = "Bucuresti-Straulesti", City = "Bucuresti" },
                new Store { Id = "5010", Name = "Targoviste-Magrini", City = "Targoviste" },
                new Store { Id = "3610", Name = "Roman-Mihai Viteazul", City = "Roman" },
                new Store { Id = "3710", Name = "Sovata", City = "Sovata" },
                new Store { Id = "6470", Name = "Targu Neamt", City = "Targu Neamt" },
                new Store { Id = "5310", Name = "Sibiu-Veteranilor de Razboi", City = "Sibiu" },
                new Store { Id = "3810", Name = "Piatra Neamt-Bistritei", City = "Piatra Neamt" },
                new Store { Id = "8470", Name = "Tarnaveni", City = "Tarnaveni" },
                new Store { Id = "8670", Name = "Timisoara-Calea Stan Vidrighin", City = "Timisoara" },
                new Store { Id = "3410", Name = "Voluntari", City = "Voluntari" },
                new Store { Id = "5710", Name = "Constanta - Palazu Mare", City = "Constanta" },
                new Store { Id = "3010", Name = "Blaj", City = "Blaj" },
                new Store { Id = "3470", Name = "Timisoara-Calea Martirilor", City = "Timisoara" },
                new Store { Id = "5110", Name = "Bals", City = "Bals" },
                new Store { Id = "4410", Name = "București Turnu Măgurele", City = "Bucuresti" },
                new Store { Id = "5910", Name = "Turnu Măgurele", City = "Turnu Magurele" },
            };
        }
        #endregion

    }
}
