using AngleSharp;
using best_discount.Modules;
using Newtonsoft.Json;

namespace best_discount
{
	internal class Program
	{
        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
            //GetCatalogs().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            var aggregatedResults = new Dictionary<string, Dictionary<string, List<Product>>>();

            // Scrape catalogs
            await GetCatalogs();

            // Scrape Lidl
            var lidlResults = await Lidl.ScrapeAsync();
            aggregatedResults.Add("Lidl", lidlResults);

            // Scrape Kaufland
           var kauflandResults = await Kaufland.ScrapeAsync();
            aggregatedResults.Add("Kaufland", kauflandResults);

            // Scrape Penny
            var pennyResults = await Penny.ScrapeAsync();
            aggregatedResults.Add("Penny", pennyResults);

            // Scrape Auchan
            var auchanResults = await Auchan.ScrapeAsync();
            aggregatedResults.Add("Auchan", auchanResults);

            // Scrape MegaImage
            var megaResults = await MegaImage.ScrapeAsync();
            aggregatedResults.Add("MegaImage", megaResults);

            // Scrape Profi
            var profiResults = await Profi.ScrapeAsync();
            aggregatedResults.Add("Profi", profiResults);

            // Serialize to JSON & save to file
            var json = JsonConvert.SerializeObject(aggregatedResults, Formatting.Indented);
            var dateNow = DateTime.Now.ToString("ddMM-HH-mm-ss");
            File.WriteAllText($"offers_{dateNow}.json", json);
        }

        static async Task GetCatalogs()
        {
            var aggregatedResults = new Dictionary<string, Dictionary<string, List<Catalog>>>();
            
            var lidlResults = await Lidl.GetCatalog();
            var kauflandResults = await Kaufland.GetCatalog();

            if (!aggregatedResults.ContainsKey("Catalogs"))
            {
                aggregatedResults["Catalogs"] = new Dictionary<string, List<Catalog>>();
            }

            aggregatedResults["Catalogs"].Add("Lidl", lidlResults);
            aggregatedResults["Catalogs"].Add("Kaufland", kauflandResults);

            var json = JsonConvert.SerializeObject(aggregatedResults, Formatting.Indented);
            var dateNow = DateTime.Now.ToString("ddMM-HH-mm-ss");
            File.WriteAllText($"catalogs_{dateNow}.json", json);
        }
    }

	
}
