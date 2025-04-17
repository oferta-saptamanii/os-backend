using best_discount.Models;
using best_discount.Utilities;
using Google.Cloud.AIPlatform.V1;
using Google.Cloud.Firestore;
using Google.Protobuf.Collections;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Google.Cloud.AIPlatform.V1.SafetySetting.Types;
using static System.Net.Mime.MediaTypeNames;

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

            foreach (var storeKv in aggregatedResults)
            {
                var storeName = storeKv.Key;
                var storeCategories = storeKv.Value;

                var storeDocRef = _db.Collection("OffersDev").Document(storeName);
                await storeDocRef.SetAsync(new { ok_field = "this does not exist" }, SetOptions.MergeAll);

                if (storeName == "Aggregated")
                {
                    foreach (var catKv in storeCategories)
                    {
                        string categoryName = catKv.Key;
                        var productsList = catKv.Value;

                        // OffersDev/Aggregated/categories/<CategoryName>
                        var categoryDocRef = storeDocRef
                            .Collection("categories")
                            .Document(categoryName.Replace("/", "-"));

                        string categoryImg = "https://images.down.monster/XUDA2/LATAfUti57.jpg/raw"; // default fallback
                        if (Utils.AggregatedCategoryImages.TryGetValue(categoryName, out var foundImg))
                        {
                            categoryImg = foundImg;
                        }

                        await categoryDocRef.SetAsync(
                            new { ProductImg = categoryImg },
                            SetOptions.MergeAll
                        );

                        foreach (var product in productsList)
                        {
                            if (string.IsNullOrWhiteSpace(product?.FullTitle))
                                continue;

                            product.Location = storeName;

                            string sanitizedTitle = product.FullTitle.Replace("/", "-");

                            // OffersDev/Aggregated/categories/<CategoryName>/products/<sanitizedTitle>
                            DocumentReference productDocRef = categoryDocRef
                                .Collection("products")
                                .Document(sanitizedTitle);

                            // await productDocRef.SetAsync(new { ok_field = "this does not exist" }, SetOptions.MergeAll);

                            await productDocRef.SetAsync(product);
                        }
                    }
                }
                else
                {
                    foreach (var catKv in storeCategories)
                    {
                        string categoryName = catKv.Key;
                        var productsList = catKv.Value;

                        // OffersDev/<StoreName>/categories/<CategoryName>
                        var categoryDocRef = storeDocRef
                            .Collection("categories")
                            .Document(categoryName.Replace("/", "-"));

                        if (storeName != "Lidl")
                        {
                            await categoryDocRef.SetAsync(
                                new { ProductImg = Utils.AssignProductImg(categoryName) },
                                SetOptions.MergeAll
                            );
                        }
                        else
                        {
                            await categoryDocRef.SetAsync(
                                new { ProductImg = productsList?.FirstOrDefault()?.ProductImg },
                                SetOptions.MergeAll
                            );
                        }

                        foreach (var product in productsList)
                        {
                            if (string.IsNullOrWhiteSpace(product?.FullTitle))
                                continue;

                            product.Location = storeName;

                            // doc => product.FullTitle sanitized
                            string sanitizedTitle = product.FullTitle.Replace("/", "-");
                            var productDocRef = categoryDocRef
                                .Collection("products")
                                .Document(sanitizedTitle);

                            await productDocRef.SetAsync(
                                new { ok_field = "this does not exist" },
                                SetOptions.MergeAll
                            );

                            await productDocRef.SetAsync(product);
                        }
                    }
                }
            }

            Console.WriteLine("Finished saving products to Firestore.");
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


        public class AiResult
        {
            [JsonProperty("product_name")]
            public string ProductName { get; set; }

            [JsonProperty("category")]
            public string Category { get; set; }

            [JsonProperty("subcategory")]
            public string Subcategory { get; set; }
        }

        public static async Task<List<AiResult>> Queryy(
    List<string> products,
    string projectId = "oferta-saptamanii",
    string location = "us-central1",
    string publisher = "google",
    string model = "gemini-2.0-flash-lite")
        {
            var predictionServiceClient = new PredictionServiceClientBuilder
            {
                Endpoint = $"{location}-aiplatform.googleapis.com"
            }.Build();

            // prompt header
            string fixedPrompt = @$"
Given these categories/subcategories in JSON:

{System.Text.Json.JsonSerializer.Serialize(ScrapingService.InitializeCategories())}

For each product below, map it to one category and one subcategory from above, 
returning a JSON array of objects. The output schema should be:

[
  {{
    ""product_name"": ""some product"",
    ""category"": ""must match EXACTLY one key from the dictionary, no diacritics!"",
    ""subcategory"": ""must match EXACTLY one subcategory from that category's list, 
                      or the last subcategory if uncertain""
  }},
  ...
]

If a product doesn't fit any known category or subcategory, use ""Unknown"" for both.

DO NOT return anything else (no explanations, no extra lines). 
Categories must be plain ASCII (no diacritics) exactly as in the dictionary keys.
Subcategories must also match exactly (no diacritics).
";

            File.WriteAllText("prompted.txt", fixedPrompt);

            int fixedTokens = 2000;
            int maxTokens = 8192;
            int availableTokens = maxTokens - fixedTokens;

            var chunkedLines = products
                .Select((p, idx) => new { p, idx })
                .GroupBy(x => x.idx / 50)
                .Select(g => string.Join("\n", g.Select(x => x.p)))
                .ToList();

            List<AiResult> aiResultsAll = new();
            int iteration = 0;

            foreach (var chunk in chunkedLines)
            {
                iteration++;
                Console.WriteLine($"Iteration {iteration}/{chunkedLines.Count}");

                string prompt = $"{fixedPrompt}\n{chunk}";

                var generateContentRequest = new GenerateContentRequest
                {
                    Model = $"projects/{projectId}/locations/{location}/publishers/{publisher}/models/{model}",
                    GenerationConfig = new GenerationConfig
                    {
                        MaxOutputTokens = 8192,
                        Temperature = 1,
                    },
                    Contents =
            {
                new Content
                {
                    Role = "USER",
                    Parts = { new Part { Text = prompt } }
                }
            },
                    SafetySettings =
            {
                new SafetySetting
                {
                    Category = HarmCategory.HateSpeech,
                    Threshold = HarmBlockThreshold.BlockOnlyHigh
                },
                new SafetySetting
                {
                    Category = HarmCategory.DangerousContent,
                    Threshold = HarmBlockThreshold.BlockOnlyHigh
                },
                new SafetySetting
                {
                    Category = HarmCategory.SexuallyExplicit,
                    Threshold = HarmBlockThreshold.BlockOnlyHigh
                },
                new SafetySetting
                {
                    Category = HarmCategory.Harassment,
                    Threshold = HarmBlockThreshold.BlockOnlyHigh
                }
            }
                };

                GenerateContentResponse response =
                    await predictionServiceClient.GenerateContentAsync(generateContentRequest);

                string responseText =
                    response.Candidates[0].Content.Parts[0].Text
                            .Replace("```json", "")
                            .Replace("```", "");


                try
                {
                    var chunkResults = JsonConvert.DeserializeObject<List<AiResult>>(responseText);
                    if (chunkResults != null)
                    {
                        aiResultsAll.AddRange(chunkResults);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to parse AI JSON: " + ex.Message);
                    return aiResultsAll;
                }

                await Task.Delay(30000);
            }

            File.WriteAllText("final-aip.txt", JsonConvert.SerializeObject(aiResultsAll, Formatting.Indented));

            return aiResultsAll;
        }




    }

}
