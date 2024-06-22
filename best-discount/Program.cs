using best_discount.Models;
using best_discount.Services;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;

namespace best_discount
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await MainAsync();
        }

        static async Task MainAsync()
        {
            var credential = GoogleCredential.FromFile("data.json");
            var builder = new FirestoreClientBuilder
            {
                Credential = credential
            };
            var client = await builder.BuildAsync();
            var db = await FirestoreDb.CreateAsync("oferta-saptamanii", client);

            var scrapingService = new ScrapingService();
            var firestoreService = new FirestoreService(db);

            // Scrape and save catalogs
            var catalogResults = await scrapingService.GetCatalogs();
            await firestoreService.SaveCatalogs(catalogResults);

            // Scrape and save products
            var productResults = await scrapingService.GetProducts();
            await firestoreService.SaveProducts(productResults);
        }
    }
}
