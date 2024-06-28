using best_discount.Models;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace best_discount.Services
{
    public class FirestoreService
    {
        private readonly FirestoreDb _db;

        public FirestoreService(FirestoreDb db)
        {
            _db = db;
        }

        public async Task SaveProducts(Dictionary<string, Dictionary<string, List<Product>>> aggregatedResults)
        {
            Console.WriteLine("Saving products to Firestore...");
            foreach (var store in aggregatedResults)
            {
                DocumentReference storeDocRef = _db.Collection("Offers").Document(store.Key);

                await storeDocRef.SetAsync(new { ok_field = "this does not exist" }, SetOptions.MergeAll);

                foreach (var category in store.Value)
                {
                    string sanitizedCategoryKey = category.Key.Replace("/", "-");
                    DocumentReference categoryDocRef = storeDocRef.Collection("categories").Document(sanitizedCategoryKey);

                    await categoryDocRef.SetAsync(new { ok_field = "this does not exist" }, SetOptions.MergeAll);

                    foreach (var product in category.Value)
                    {
                        string sanitizedTitle = product.FullTitle.Replace("/", "-");
                        DocumentReference productDocRef = categoryDocRef.Collection("products").Document(sanitizedTitle);
                        await productDocRef.SetAsync(new { ok_field = "this does not exist" }, SetOptions.MergeAll);

                        await productDocRef.SetAsync(product);
                    }
                }
            }
        }


        public async Task SaveCatalogs(Dictionary<string, Dictionary<string, Dictionary<string, List<Catalog>>>> catalogResults)
        {
            Console.WriteLine("Saving catalogs to Firestore...");
            foreach (var store in catalogResults)
            {
                DocumentReference storeDocRef = _db.Collection("Catalogs").Document(store.Key);
                await storeDocRef.SetAsync(new { ok_field = "this does not exist" }, SetOptions.MergeAll);

                foreach (var city in store.Value)
                {
                    DocumentReference cityDocRef = storeDocRef.Collection("cities").Document(city.Key);
                    await cityDocRef.SetAsync(new { ok_field = "this does not exist" }, SetOptions.MergeAll);

                    foreach (var storeName in city.Value)
                    {
                        DocumentReference storeNameDocRef = cityDocRef.Collection("stores").Document(storeName.Key);
                        await storeNameDocRef.SetAsync(new { ok_field = "this does not exist" }, SetOptions.MergeAll);

                        foreach (var catalog in storeName.Value)
                        {
                            string sanitizedCatalogName = catalog.Name.Replace("/", "-");
                            DocumentReference catalogDocRef = storeNameDocRef.Collection("catalogs").Document(sanitizedCatalogName);
                            await catalogDocRef.SetAsync(new { ok_field = "this does not exist" }, SetOptions.MergeAll);

                            await catalogDocRef.SetAsync(catalog);
                        }
                    }
                }
            }
        }

    }

}
