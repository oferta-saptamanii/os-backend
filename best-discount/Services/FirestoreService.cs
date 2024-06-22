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

                foreach (var category in store.Value)
                {
                    string sanitizedCategoryKey = category.Key.Replace("/", "-");
                    DocumentReference categoryDocRef = storeDocRef.Collection("categories").Document(sanitizedCategoryKey);

                    foreach (var product in category.Value)
                    {
                        CollectionReference productsColRef = categoryDocRef.Collection("products");
                        await productsColRef.AddAsync(product);
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

                foreach (var city in store.Value)
                {
                    DocumentReference cityDocRef = storeDocRef.Collection("cities").Document(city.Key);

                    foreach (var storeName in city.Value)
                    {
                        DocumentReference storeNameDocRef = cityDocRef.Collection("stores").Document(storeName.Key);
                        CollectionReference catalogColRef = storeNameDocRef.Collection("catalogs");

                        foreach (var catalog in storeName.Value)
                        {
                            await catalogColRef.AddAsync(catalog);
                        }
                    }
                }
            }
        }
    }

}
