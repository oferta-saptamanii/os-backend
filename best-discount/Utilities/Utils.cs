using AngleSharp.Dom;
using AngleSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using SixLabors.ImageSharp.Formats.Png;
using System.Text.RegularExpressions;

namespace best_discount.Utilities
{
    public static class Utils
    {
        public static string ConvertToCurrency(string priceString)
        {
            if (string.IsNullOrEmpty(priceString) || !int.TryParse(priceString, out int priceInt))
            {
                return null;
            }

            decimal priceDecimal = priceInt / 100m;
            return priceDecimal.ToString("F2", CultureInfo.InvariantCulture);
        }

        public enum ErrorType
        {
            ERROR,
            EXCEPTION
        }

        public static void Report(
        string message,
        ErrorType type,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
        {
            string className = Path.GetFileNameWithoutExtension(filePath);
            string errorType = type == ErrorType.ERROR ? "ERROR" : "EXCEPTION";

            Console.WriteLine($"{className}:{lineNumber} {memberName}: {errorType} - {message}");


            // Mail / push notification the devs
            // ....
        }

        public static async Task<string> FetchContent(HttpClient client, string url)
        {
            HttpResponseMessage response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var contentStream = await response.Content.ReadAsStreamAsync();
            string contentEncoding = response.Content.Headers.ContentEncoding.FirstOrDefault();

            if (contentEncoding == "gzip")
            {
                using (var decompressedStream = new GZipStream(contentStream, CompressionMode.Decompress))
                using (var reader = new StreamReader(decompressedStream))
                {
                    return await reader.ReadToEndAsync();
                }
            }
            else if (contentEncoding == "deflate")
            {
                using (var decompressedStream = new DeflateStream(contentStream, CompressionMode.Decompress))
                using (var reader = new StreamReader(decompressedStream))
                {
                    return await reader.ReadToEndAsync();
                }
            }
            else
                return await response.Content.ReadAsStringAsync();
        }

        public static async Task<IDocument> ParseHtml(string htmlContent)
        {
            var config = Configuration.Default.WithDefaultLoader().WithXPath();
            var context = BrowsingContext.New(config);
            return await context.OpenAsync(req => req.Content(htmlContent));
        }

        public static async Task DownloadImage(string imageUrl, string destinationPath)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(imageUrl);
                if (response.IsSuccessStatusCode)
                {
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        using (var fileStream = new FileStream(destinationPath, FileMode.Create))
                        {
                            await stream.CopyToAsync(fileStream);
                        }
                    }
                }
            }
        }

        public static void PdfFromImages(List<string> imagePaths, string pdfFilePath)
        {
            using (PdfDocument pdf = new PdfDocument())
            {
                try
                {
                    foreach (var imagePath in imagePaths)
                    {
                        using (var image = SixLabors.ImageSharp.Image.Load(imagePath))
                        {
                            using (var ms = new MemoryStream())
                            {
                                image.Save(ms, new PngEncoder());  // Convert to PNG and save to MemoryStream
                                ms.Seek(0, SeekOrigin.Begin);  // Reset MemoryStream position

                                PdfPage page = pdf.AddPage();
                                using (XGraphics gfx = XGraphics.FromPdfPage(page))
                                {
                                    // Creating a function that returns the stream
                                    Func<Stream> streamFunc = () => new MemoryStream(ms.ToArray());
                                    using (XImage xImage = XImage.FromStream(streamFunc))
                                    {
                                        gfx.DrawImage(xImage, 0, 0, page.Width, page.Height);
                                    }
                                }
                            }
                        }
                    }
                    pdf.Save(pdfFilePath);
                    Console.WriteLine($"PDF saved: {pdfFilePath}");
                }
                catch (Exception ex)
                {
                    Utils.Report($"An error occurred while saving the PDF: {ex.Message}", ErrorType.EXCEPTION);
                }
            }
        }

        public static List<string[]> SplitInChunks(string[] array, int chunkSize)
        {
            var chunks = new List<string[]>();
            for (int i = 0; i < array.Length; i += chunkSize)
            {
                int size = Math.Min(chunkSize, array.Length - i);
                chunks.Add(array.Skip(i).Take(size).ToArray());
            }
            return chunks;
        }

        public static string RemoveEmptyAndWhitespaceOnlyLines(string input)
        {
            var lines = input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var nonEmptyLines = lines.Where(line => !string.IsNullOrWhiteSpace(line));
            return string.Join("\n", nonEmptyLines);
        }

        public static string GetQuantity(string title)
        {
            string pattern = @"\b\d+[.,]?\d*\s?(L|ml|bucati|g|kg)\b|(\d+\+\d+\s?extra)";
            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);

            Match match = regex.Match(title);
            return match.Success ? match.Value : "1";
        }

        // temporary dev fix, need a better way for this
        public static string AssignProductImg(string category)
        {
            var mapping = new Dictionary<string, string>()
            {
                // auchan
                { "Cafea si Ceai", "CoffeeTea" },
                { "Bauturi alcoolice", "Alcohol" },
                { "Bauturi Racoritoare", "Beverages" },
                { "Alimentatie speciala", "SpecialFood" },
                { "Alimente", "Food" },
                { "Lactate si Mezeluri", "DairyMeat" },
                { "Dulciuri", "Candies" },
                { "Gustari Sarate", "Snacks" },
                { "Sampon si Gel de dus", "ShampooShower" },
                { "Deodorante", "Deodorant" },
                { "Igiena Orala", "OralHygiene" },
                { "Lotiuni, Creme si Igiena intima", "PersonalCare" },
                { "Detergent si balsam rufe", "Detergent" },
                { "Produse si curatiene", "CleaningProducts" },
                { "Hârtie igienica si Menaj", "HouseholdPaper" },
                { "Produse coafat", "Hairdress" },
                { "Bebe", "Baby" },
                { "Pet Shop", "Pet" },
                { "Vase de gatit", "Cookware" },
                { "Auto", "Auto" },

                // kaufland
                { "Alimente de bază", "Food" },
                { "Animale", "Pet" },
                { "Auto, sport, timp liber", "Auto" },
                { "Bebeluși, copii, jucării", "Baby" },
                { "Brutărie", "Bakery" },
                { "Băuturi", "Beverages" },
                { "Cafea și ceai", "CoffeeTea" },
                { "Carne, mezeluri", "DairyMeat" },
                { "Cosmetice, îngrijire", "PersonalCare" },
                { "Curățenie, detergenți", "CleaningProducts" },
                { "Delicatese, congelate", "FrozenFood" },
                { "Dulciuri, snackuri", "CandiesSnacks" },
                { "Electro, birou", "ElectronicsOffice" },
                { "Gospodărie", "Household" },
                { "Haine, pantofi, accesorii", "Fashion" },
                { "Lactate, ouă", "DairyEggs" },
                { "Legume, fructe, flori", "FruitsVegetables" },
                { "Pește", "Fish" },
                { "Peste", "Fish" },

                // megaimage
                { "Animale de companie", "Pet" },
                { "Apa si sucuri", "Beverages" },
                { "Cosmetice si ingrijire personala", "PersonalCare" },
                { "Curatenie si nealimentare", "CleaningProducts" },
                { "Dulciuri si snacks", "CandiesSnacks" },
                { "Equilibrium", "SpecialFood" },
                { "Fructe si legume proaspete", "FruitsVegetables" },
                { "Ingrediente culinare", "CulinaryIngredients" },
                { "Lactate si oua", "DairyEggs" },
                { "Mama si ingrijire copil", "Baby" },
                { "Mezeluri, carne si ready meal", "DairyMeat" },
                { "Paine, cafea, cereale si mic dejun", "Bakery" },
                { "Produse congelate", "FrozenFood" },
                { "Produse sezoniere", "SeasonalProducts" },
                //ceva
                { "Apa", "Beverages" },
                { "Absorbante", "PersonalCare" },
                { "CONDIMENTE,SARE,PROD INSTANT", "CulinaryIngredients" },
                { "Conserve carne si pate", "CannedPork" },
                { "Conserve fructe, compot", "CannedFruits" },

                // penny
                { "Alune si seminte", "NutsSeeds" },
                { "Batoane ciocolata", "ChocolateBars" },
                { "Bere blonda si bruna", "Beer" },
                { "Biscuiti, fursecuri, prajituri", "CookiesPastries" },
                { "Cafea", "CoffeeTea" },
                { "Carne proaspata porc si vita", "MeatPorkBeef" },
                { "Carne proaspata pui si curcan", "MeatChickenTurkey" },
                { "Cascaval", "Cheese" },
                { "Ceai, cacao", "CoffeeTea" },
                { "Chipsuri", "Snacks" },
                { "Conserve legume", "CannedVegetables" },
                { "Conserve peste", "CannedFish" },
                { "Covrigei,pufuleti si mixuri", "Snacks" },
                { "Crema de branza, specialitati", "Cheese" },
                { "Cremwursti si parizer", "DairyMeat" },
                { "DETERGENT SUPRAFETE,ANTICALCAR", "CleaningProducts" },
                { "Deodorant", "Deodorant" },
                { "Detergent, balsam si inalbitor", "Detergent" },
                { "Dulceata, creme tartinabile", "Spreads" },
                { "Energizante", "EnergyDrinks" },
                { "Faina, malai, gris", "Flour" },
                { "Fructe proaspete", "FruitsVegetables" },
                { "Gata preparate", "ReadyMeals" },
                { "Iaurt", "Dairy" },
                { "Inghetata", "FrozenFood" },
                { "Ingrediente patiserie", "BakeryIngredients" },
                { "Ingrijire fata si maini", "PersonalCare" },
                { "Lapte batut, sana si chefir", "Dairy" },
                { "Lapte proaspat si UHT", "Dairy" },
                { "Legume, salate si verdeturi", "FruitsVegetables" },
                { "Mustar si ketchup", "Sauces" },
                { "Napolitane si nuga", "CandiesSnacks" },
                { "Odorizant camera si toaleta", "CleaningProducts" },
                { "Orez, fasole, arpacas, soia", "Grains" },
                { "Pasta de rosii, bulion", "Sauces" },
                { "Paste fainoase", "Pasta" },
                { "Patiserie", "Bakery" },
                { "Peste si icre", "Fish" },
                { "Praline si bomboane", "CandiesSnacks" },
                { "Pui- vita", "MeatChickenBeef" },
                { "Salam si carnati", "DairyMeat" },
                { "Sampon si balsam", "ShampooShower" },
                { "Semipreparate", "SemiPreparedFood" },
                { "Servetele universale si role", "HouseholdPaper" },
                { "Sosuri", "Sauces" },
                { "Spirtoase", "Alcohol" },
                { "Sucuri carbogazoase", "Beverages" },
                { "Sucuri necarbogazoase", "Beverages" },
                { "Sunca si alte specialitati", "DairyMeat" },
                { "Telemea", "Cheese" },
                { "Unica folosinta si menaj", "HouseholdPaper" },
                { "Unt si margarina", "ButterMargarine" },
                { "Vin", "Alcohol" },

                // profi
                { "Oferte Speciale" , "OferteSpeciale"},
                { "Top Oferte", "TopOferte" }
            };

            string cat;
            if (mapping.TryGetValue(category, out cat))
            {
                string imageUrl = GetCategoryImg(cat);
                return imageUrl;
            }

            return null;
        }
        static string GetCategoryImg(string commonCategory)
        {
            var categoryImageUrls = new Dictionary<string, string>()
     {
         { "CoffeeTea", "https://images.down.monster/XUDA2/PEPEHEvO31.jpg/raw" },
         { "Alcohol", "https://images.down.monster/XUDA2/HAKacOGU15.jpg/raw" },
         { "Beverages", "https://images.down.monster/XUDA2/SifUpiza56.jpg/raw" },
         { "SpecialFood", "" },
         { "Food", "https://images.down.monster/XUDA2/DiKuLali28.jpg/raw" },
         { "DairyMeat", "https://images.down.monster/XUDA2/ziSONato58.jpg/raw" },
         { "Candies", "https://images.down.monster/XUDA2/wUkIjolo82.jpg/raw" },
         { "Snacks", "https://images.down.monster/XUDA2/VElaVESe91.jpg/raw" },
         { "ShampooShower", "https://images.down.monster/XUDA2/KobUSIQA95.jpg/raw" },
         { "Deodorant", "https://images.down.monster/XUDA2/VimOwAFU00.jpg/raw" },
         { "OralHygiene", "https://images.down.monster/XUDA2/guQoZITe65.jpg/raw" },
         { "PersonalCare", "https://images.down.monster/XUDA2/KAGewizO31.jpg/raw" },
         { "Detergent", "https://images.down.monster/XUDA2/SavaLaVA60.jpg/raw" },
         { "CleaningProducts", "https://images.down.monster/XUDA2/ZolACuMU10.jpg/raw" },
         { "HouseholdPaper", "https://images.down.monster/XUDA2/kanEButi88.jpg/raw" },
         { "Hairdress", "https://images.down.monster/XUDA2/SIdabecA28.jpg/raw" },
         { "Baby", "https://images.down.monster/XUDA2/koFONohI13.jpg/raw" },
         { "Pet", "https://images.down.monster/XUDA2/XuriteLO30.jpg/raw" },
         { "Cookware", "https://images.down.monster/XUDA2/DAfEvohe75.jpg/raw" },
         { "Auto", "https://images.down.monster/XUDA2/gEVEBonu43.jpg/raw" },
         { "Bakery", "https://images.down.monster/XUDA2/lOhAXojA62.jpg/raw" },
         { "FrozenFood", "https://images.down.monster/XUDA2/RImEjOLe37.jpg/raw" },
         { "CandiesSnacks", "https://images.down.monster/XUDA2/wUkIjolo82.jpg/raw" },
         { "CannedFruits", "" },
         { "CannedPork", "" },
         { "ElectronicsOffice", "" },
         { "Household", "https://images.down.monster/XUDA2/SiwOMUNi48.jpg/raw" },
         { "Fashion", "https://images.down.monster/XUDA2/TEPIVObO04.jpg/raw" },
         { "DairyEggs", "https://images.down.monster/XUDA2/zUZOJORU08.jpg/raw" },
         { "FruitsVegetables", "https://images.down.monster/XUDA2/juRUValE33.jpg/raw" },
         { "Fish", "https://images.down.monster/XUDA2/LAqiXERA84.jpg/raw" },
         { "CulinaryIngredients", "https://images.down.monster/XUDA2/wafoKeje27.jpg/raw" },
         { "SeasonalProducts", "" },
         { "NutsSeeds", "https://images.down.monster/XUDA2/gIwaHiWA97.jpg/raw" },
         { "ChocolateBars", "https://images.down.monster/XUDA2/BeRISegI34.jpg/raw" },
         { "Beer", "https://images.down.monster/XUDA2/palOdare35.jpg/raw" },
         { "CookiesPastries", "https://images.down.monster/XUDA2/BeyiTAJU89.jpg/raw" },
         { "MeatPorkBeef", "https://images.down.monster/XUDA2/VevOSipo57.jpg/raw" },
         { "MeatChickenTurkey", "https://images.down.monster/XUDA2/qoQaQOGa50.jpg/raw" },
         { "Cheese", "https://images.down.monster/XUDA2/nIGiyuMA68.jpg/raw" },
         { "CannedVegetables", "https://images.down.monster/XUDA2/VihaqemE12.jpg/raw" },
         { "CannedFish", "https://images.down.monster/XUDA2/VuFecUNi41.jpg/raw" },
         { "Spreads", "https://images.down.monster/XUDA2/nIYoXeTA15.jpg/raw" },
         { "EnergyDrinks", "https://images.down.monster/XUDA2/HacIhaki62.jpg/raw" },
         { "Flour", "https://images.down.monster/XUDA2/KicUGEfA00.jpg/raw" },
         { "ReadyMeals", "" },
         { "Dairy", "https://images.down.monster/XUDA2/FEQUhaJU82.jpg/raw" },
         { "BakeryIngredients", "" },
         { "Sauces", "https://images.down.monster/XUDA2/QEmAnali17.jpg/raw" },
         { "Grains", "https://images.down.monster/XUDA2/bepeMuzO19.jpg/raw" },
         { "Unknown", "" },
         { "Pasta", "https://images.down.monster/XUDA2/pEKeMAmI16.jpg/raw" },
         { "MeatChickenBeef", "" },
         { "SemiPreparedFood", "" },
         { "ButterMargarine", "https://images.down.monster/XUDA2/xInItAgi36.jpg/raw" },
         { "OferteSpeciale" , "https://images.down.monster/rAjo2/bugINaCa23.png/raw"},
         { "TopOferte", "https://images.down.monster/rAjo2/bugINaCa23.png/raw" }
     };

            string imageUrl;
            if (categoryImageUrls.TryGetValue(commonCategory, out imageUrl))
                return imageUrl;


            return null;
        }

        public static readonly Dictionary<string, string> AggregatedCategoryImages = new()
        {
            { "Auto, Gradina si Bricolaj", "https://images.down.monster/XUDA2/gEVEBonu43.jpg/raw" },
            { "Bacanie", "https://images.down.monster/XUDA2/VimApocU26.jpg/raw" },
            { "Bauturi si Tutun", "https://images.down.monster/XUDA2/zeDuQUye95.jpg/raw" },
            { "Bebe", "https://images.down.monster/XUDA2/BOgUgErA73.jpg/raw" },
            { "Brutarie, Cofetarie, Gastro", "https://images.down.monster/XUDA2/tUZoCIBi24.jpg/raw" },
            { "Casa si Curatenie", "https://images.down.monster/XUDA2/LIfORAJi43.jpg/raw" },
            { "Cosmetice si ingrijire personala", "https://images.down.monster/XUDA2/CEpOfeWi11.jpg/raw" },
            { "Electrocasnice si Climatizare", "https://images.down.monster/XUDA2/JiMENiri99.jpg/raw" },
            { "Fructe si Legume", "https://images.down.monster/XUDA2/juRUValE33.jpg/raw" },
            { "Jucarii si Timp Liber", "https://images.down.monster/XUDA2/RUXoVANa29.jpg/raw" },
            { "Lactate, Carne, Mezeluri & Peste", "https://images.down.monster/XUDA2/NAtOxEBe28.jpg/raw" },
            { "Altele", "https://images.down.monster/XUDA2/LATAfUti57.jpg/raw" },
            { "Pet Shop", "https://images.down.monster/XUDA2/VAduMIcA07.jpg/raw" },
            { "Telefoane, Tablete, Electronice si IT", "https://images.down.monster/XUDA2/FEGiHuTi86.jpg/raw" },
        };


    }
}
