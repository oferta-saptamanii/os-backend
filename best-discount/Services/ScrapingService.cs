using best_discount.Models;
using best_discount.Modules;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace best_discount.Services
{
    public class ScrapingService
    {
        private readonly SeleniumService _seleniumService;
        public ScrapingService()
        {
            _seleniumService = new SeleniumService();
        }

        public async Task<Dictionary<string, Dictionary<string, List<Product>>>> GetProducts()
        {
            var aggregatedResults = new Dictionary<string, Dictionary<string, List<Product>>>();

            // Scrape Lidl
            var lidlResults = await Lidl.ScrapeAsync();
            aggregatedResults.Add("Lidl", lidlResults);

            // Scrape Penny
            var pennyResults = await Penny.ScrapeAsync();
            aggregatedResults.Add("Penny", pennyResults);

            // Scrape Auchan
            var auchanResults = await Auchan.ScrapeAsync();
            aggregatedResults.Add("Auchan", auchanResults);

            // Scrape Kaufland
            var kauflandResults = await Kaufland.ScrapeAsync();
            aggregatedResults.Add("Kaufland", kauflandResults);

            // Scrape MegaImage
            var megaResults = await MegaImage.ScrapeAsync();
            aggregatedResults.Add("MegaImage", megaResults);

            // Scrape Profi
            var profiResults = await Profi.ScrapeAsync();
            aggregatedResults.Add("Profi", profiResults);

            return aggregatedResults;
        }

        public async Task<Dictionary<string, Dictionary<string, Dictionary<string, List<Catalog>>>>> GetCatalogs()
        {
            var aggregatedResults = new Dictionary<string, Dictionary<string, Dictionary<string, List<Catalog>>>>();

            var lidlResults = Transform(await Lidl.GetCatalog(), "Lidl");
            var kauflandResults = await Kaufland.GetAllCatalogs();
            var auchanResults = Transform(await Auchan.GetCatalog(), "Auchan");

            var profi = new Profi(_seleniumService);
            var profiResults = Transform(await profi.GetCatalog(), "Profi");

            var mega = new MegaImage(_seleniumService);
            var megaResults = Transform(await mega.GetCatalog(), "MegaImage");
            aggregatedResults.Add("Lidl", lidlResults);
            aggregatedResults.Add("Kaufland", kauflandResults);
            aggregatedResults.Add("Auchan", auchanResults);
            aggregatedResults.Add("Profi", profiResults);
            aggregatedResults.Add("MegaImage", megaResults);
            _seleniumService.Quit();
            return aggregatedResults;
        }

        private static Dictionary<string, Dictionary<string, List<Catalog>>> Transform(List<Catalog> catalogs, string storeName)
        {
            var result = new Dictionary<string, Dictionary<string, List<Catalog>>>();

            if (catalogs.Count > 0)
            {
                string city = $"{storeName} City";
                string name = $"{storeName} Store";

                result[city] = new Dictionary<string, List<Catalog>>();
                result[city][name] = catalogs;
            }

            return result;
        }
    }
}
