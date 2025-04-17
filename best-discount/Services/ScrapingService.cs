using best_discount.Models;
using best_discount.Modules;
using best_discount.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

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

            var stores = new List<(string name, Task<Dictionary<string, List<Product>>> task)>
            {
                ("Lidl", Lidl.ScrapeAsync()),
                ("Penny", Penny.ScrapeAsync()),
                ("Auchan", Auchan.ScrapeAsync()),
                ("Kaufland", Kaufland.ScrapeAsync()),
                ("MegaImage", MegaImage.ScrapeAsync()),
                ("Profi", Profi.ScrapeAsync())
            };

            var allProducts = new List<Product>();

            foreach (var (storeName, task) in stores)
            {
                var storeResults = await task;
                aggregatedResults[storeName] = storeResults;

                foreach (var kv in storeResults)
                {
                    allProducts.AddRange(kv.Value);
                }
            }

            var productsArray = allProducts
                .Select(p => WebUtility.HtmlDecode(p.FullTitle))
                .ToList();

            var aiData = await FirestoreService.Queryy(productsArray);
            var aiMapping = new Dictionary<string, (string category, string subcategory)>();
            foreach (var item in aiData)
            {
                aiMapping[item.ProductName] = (item.Category, item.Subcategory);
            }

            var predefinedCategories = InitializeCategories();
            var aggregatedStore = new Dictionary<string, List<Product>>();

            foreach (var catKey in predefinedCategories.Keys)
            {
                aggregatedStore[catKey] = new List<Product>();
            }

            if (!aggregatedStore.ContainsKey("Altele"))
            {
                aggregatedStore["Altele"] = new List<Product>();
            }

            foreach (var product in allProducts)
            {
                string rawName = WebUtility.HtmlDecode(product.FullTitle);

                string finalCategory = "Altele";
                string finalSubcategory = "Altele";

                if (!string.IsNullOrWhiteSpace(rawName) && aiMapping.ContainsKey(rawName))
                {
                    finalCategory = aiMapping[rawName].category;
                    finalSubcategory = aiMapping[rawName].subcategory;
                }

                product.Category = finalCategory;
                product.Subcategory = finalSubcategory;

                if (!aggregatedStore.ContainsKey(finalCategory))
                {
                    aggregatedStore["Altele"].Add(product);
                }
                else
                {
                    aggregatedStore[finalCategory].Add(product);
                }
            }

            aggregatedResults["Aggregated"] = aggregatedStore;

            return aggregatedResults;
        }

        public static Dictionary<string, List<string>> InitializeCategories()
        {
			var predefinedCategoriesNew = new Dictionary<string, List<string>>
{
	{
		"Lactate, Carne, Mezeluri & Peste",  new List<string>
		{
			// Lactate
            "Iaurt",
			"Lapte",
            "Sana, Chefir si Lapte batut", 
            "Branza si Telemea", 
            "Cascaval", 
            "Branzeturi tartinabile/feliate", 
            "Specialitati din lapte", 
            "Smantana si frisca", 
            "Unt si margarina", 
            "Deserturi cu lapte",
            "Alte produse din lactate",

            // Carne
			"Carne de pasare", 
            "Carne de porc", 
            "Carne de vita si manzat", 
            "Carne de oaie si miel", 
            "Specialitati din carne", 
            "Carne congelata",

            //Mezeluri
			"Salam", 
            "Sunca si Jambon", 
            "Carnati", 
            "Cremvusti", 
            "Parizer", 
            "Mezeluri feliate", 
            "Alte mezeluri si specialitati",

            //Pescarie

			"Peste proaspat", 
            "Peste congelat", 
            "Specialitati din peste", 
            "Fructe de mare",

            // Oua
            "Oua",

            "Alte produse lactate, carne, peste"
		}
	},
	{
		"Fructe si Legume", new List<string>
		{
            // Fructe
			"Fructe bio", 
            "Fructe congelate", 
            "Fructe proaspate", 
            "Fructe deshidratate",

            // Legume
			"Legume bio",
            "Legume congelate", 
            "Legume proaspate", 
            "Legume deshidratate",


			// Salate si Verdeturi
            "Salate",
			"Plante aromatice si Verdeturi",


			"Alte produse fructe si legume"
		}
	},
	{
		"Bauturi si Tutun", new List<string>
		{
            // Apa
			"Apa plata", 
            "Apa carbogazoasa",

            // Bauturi racoritoare
			"Sucuri", 
            "Siropuri", 
            "Energizante",

            // Bere si cidru
			"Bere", 
            "Cidru",

            // Vinuri
			"Vin roze", 
            "Vin alb", 
            "Vin spumant si Sampanie",

            // Bauturi aperitive si spirtoase
			"Whisky", 
            "Rom", 
            "Vodca", 
            "Gin", 
            "Tequila",
            "Tuica si Palinca",
            "Alte aperitive si digestive",

            // Tutun
            "Tutun",

            "Alte produse bauturi si tutun"
		}
	},
	{
		"Bacanie", new List<string>
		{
			//Inghetata
            "Inghetata",

            // Alimente de baza
            "Ulei",
			"Otet",
			"Zahar si Indulcitori",
			"Paste fainoase",
			"Orez si Legume uscate",
            "Alte alimente de baza",
			
            // Ceai si cafea
            "Cafea",
			"Ceai",

            // Dulciuri
            "Ciocolata",
			"Batoane ciocolata",
			"Biscuiti",
			"Napolitane",
            "Bezele si Jeleuri",
			"Dropsuri si Guma de mestecat",
			"Alte dulciuri",

            // Conserve
			"Conserve carne si Pate",
			"Conserve peste",
			"Conserve de legume",
			"Conserve mancare gatita",
			"Muraturi",
			"Compot",
			"Alte conserve",

            // Alimente sarate
			"Chipsuri",
			"Covrigi",
			"Popcorn si Pufuleti",
			"Alune, Fistic, Seminte si Mixuri",
			"Alte alimente sarate",


            // Condimente si Sosuri
            "Ketchup, Mustar si Maioneza",
			"Sos de rosii",
			"Sosuri pentru paste",
			"Sare",
			"Condimente si Mirodenii",
			"Alte Condimente si Sosuri",

            // Cereale
			"Cereale",
			"Musli",
			"Alte cereale",

            // Dulceata, Miere si Crema tartinabila
			"Magiun, Gem si Dulceata",
			"Miere",
			"Crema tartinabila",


			"Alte produse bacanie"
		}
	},
	{
		"Brutarie, Cofetarie, Gastro", new List<string>
		{
			// Paine
            "Paine",
			"Specialitati paine",
			"Chifle, Baghete si Lipii",

            // Gastro
			"Ready to eat",
			"Sushi",
			"Sandwichuri",
			"Salate si Hummus",
            "Paste proaspete",
            "Platouri",
            "Alte produse gastro",

            // Cofetarie
            "Prajituri si Eclere",
            "Torturi",
            "Tarte",
            "Alte produse cofetarie",

            // Patiserie
            "Foitaje",
            "Covrigi",
            "Checuri si Briose",
            "Cozonaci",
            "Alte produse patiserie",

            // Semipreparate
			"Semipreparate",
			"Pizza congelata",
			

            "Alte produse brutarie"
		}
	},
	{
		"Casa si Curatenie", new List<string>
		{
			// Hartie igienica, Prosoape si Servetele
            "Hartie igienica",
            "Prosoape si Role bucatarie",
            "Servetele",

            // Produse menaj
            "Saci de gunoi",
            "Lavete si Bureti de vase",
            "Pungi si folii alimentare",
            "Manusi de curatenie",

            // Detergent si Balsam de rufe
            "Detergent rufe",
            "Balsam de rufe",
            "Inalbitor si solutii pentru pete",
            "Alte produse pentru rufe",

            // Produse si Solutii curatenie
            "Curatenie bucatarie",
            "Detergent vase",
            "Curatenie baie",
            "Alte produse curatenie",

            // Intretinere casa
            "Maturi, Mopuri si Perii",
			"Cosuri si Lighenase",
			"Produse baie",
            "Uscatoare rufe",
            "Mese de calcat",
            "Alte produse intretinere casa",


            // Vesela si Accesorii bucatarie
			"Pahare",
			"Tacamuri",
			"Farfurii si Vesela",
			"Vase de gatit",
			"Ustensile de gatit",
            "Textile bucatarie",
			"Alte accesorii bucatarie",

            // Prosoape si Accesorii baie
            "Prosoape baie",
            "Prosoape plaja",
            "Halate baie",
            "Covorase baie",
            "Sanitare si Accesorii",

            // Textile si covoare
            "Perne",
            "Lenjerii de pat",
            "Covoare",
            "Alte textile si covorase",

            // Mobila si Decoratiuni
            "Mobila si Decoratiuni",

            "Alte produse casa si curatenie"
		}
	},
	{
		"Cosmetice si ingrijire personala", new List<string>
		{
			// Hair Styling
			"Vopsea pentru par",
			"Sampon pentru par",
			"Balsam pentru par",
			"Fixativ",
			"Gel de par",
			"Altele pentru par",

            // Igiena Intima
			"Absorbante",
			"Prezervative si lubrifianti",
			"Tampoane",
			"Servetele umede",
            "Altele igiena intima",

            // Igiena dentara
            "Periuta de dinti",
            "Pasta de dinti",
            "Apa de gura",
            "Altele igiena dentara",

            // Produse ingrijire
            "Deodorant",
			"Gel de dus",
			"Sapun solid",
			"Sapun lichid",
            "Alte produse ingrijire",


			"Alte produse ingrijire si cosmetice",
		}
	},
	{
		"Bebe", new List<string>
		{
			// Mancare bebe
            "Cereale si Biscuiti bebe",
            "Alte preparate bebe",

			// Lapte si bauturi bebe
            "Forume lapte bebe",
            "Apa, ceai si sucuri bebelusi",
            "Alte bauturi bebe",

            // Igiena si inrijire bebe
            "Scutece",
            "Servetele umede",
            "Igiena bebe",

			// Puericultura
            "Biberoane si tetine",
            "Suzete",
            "Alte accesorii bebelusi",

			"Jucarii Bebelusi",

            "Alte produse bebe"
		}
	},
	{
		"Jucarii si Timp Liber", new List<string>
		{
			// Jucarii
			"Puzzle",
            "Jucarii de plus",
            "Papusi si Accesorii",
            "Figurine",
            "Masinute si seturi de joaca",
            "Arme de jucarie",
            "Roboti si Trenulete",
            "Alte jucarii",

            // Sporturi
			"Biciclete",
            "Role, trotinete, skateboard",
            "Drumetie si Camping",
            "Echipamente si Accesorii fitness",
            "Pescuit, vanatoare",
            "Alte sporturi",
            
            // Papetarie
            "Produse birou",
            "Produse scoala",
            "Produse din hartie",

			// Bagaje
            "Genti voiaj",
            "Trolere",
            "Accesorii bagaje",

            "Alte produse jucarii si timp liber"
		}
	},
	{
		"Pet Shop", new List<string>
		{
			// Caini
            "Hrana si Recompoense caine",
            "Ingrijire caine",
            "Accesorii caine",

			// Pisici
            "Hrana si Recompense pisici",
            "Ingrijire pisica",
            "Accesorii pisica",

			// Pasari
            "Hrana pasari",
            "Accesorii pasari",

			// Rozatoare
            "Hrana rozatoare",
            "Accesorii rozatoare",

			// Pesti si testoase
            "Hrana pesti, testoase",
            "Accesorii pesti, testoase",

            "Alte produse pet shop"
		}
	},
	{
		"Electrocasnice si Climatizare", new List<string>
		{
			// Electrocasnice mari
            "Aragazuri si Hote",
            "Aparate frigorifice",
            "Masini de spalat si Uscatoare",
            "Cuptoare si Plite",


			// Electrocasnice mici
            "Esspresoare si Cafetiere",
            "Cuptoare cu microunde",
            "Aparate bucatarie",
            "Aspiratoare",
            "Fiare de calcat si Masini de cusut",
            "Aparate curatenie",


			// Climatizare
            "Aparate aer conditionat",
            "Purificatoare si Umidificatoare",
            "Ventilatoare",

            "Alte electrocasnice"
		}
	},
	{
		"Telefoane, Tablete, Electronice si IT", new List<string>
		{
			// Telefoane, Tablete si Wearables
            "Smartphones",
            "Accesorii tablete",

			// Electronice
            "Televizoare",
            "Accesorii televizoare",
            "Boxe si Sisteme audio",
            "Accesorii audio",

			// IT
            "Accesorii laptop",
            "Consumabile IT",

			// Gaming si Gadgets
            "Console gaming",
            "Jocuri",

            "Alte electronice"
		}
	},
	{
		"Auto, Gradina si Bricolaj", new List<string>
		{
			// Auto
            "Electronice Auto",
            "Echipamente si unelte Auto",
            "Intretinere si Cosmetica Auto",
            "Accesorii auto",

			// Bricolaj
            "Scule Electrice Bricolaj",
            "Unelte Bricolaj",
            "Adezivi si Siliconi",
            "Becuri si lanterne",
            "Baterii si incarcatoare",
            "Electrice",


            // Gradina
            "Scule Electrice Gradina",
            "Unelte Gradina",
            "Flori, Plante si Pomi",
            "Gratare",


            "Alte auto, gradina, bricolaj"
		}
	}
};


			return predefinedCategoriesNew;
        }

        public async Task<Dictionary<string, Dictionary<string, Dictionary<string, List<Catalog>>>>> GetCatalogs()
        {
            var aggregatedResults = new Dictionary<string, Dictionary<string, Dictionary<string, List<Catalog>>>>();

            var lidlResults = Transform(await Lidl.GetCatalog(), "Lidl");
            var kauflandResults = await Kaufland.GetAllCatalogs();
            var auchanResults = Transform(await Auchan.GetCatalog(), "Auchan");

            var profi = new Profi(_seleniumService);
            //var profiResults = Transform(await profi.GetCatalog(), "Profi");

            var mega = new MegaImage(_seleniumService);
            var megaResults = Transform(await mega.GetCatalog(), "MegaImage");
            aggregatedResults.Add("Lidl", lidlResults);
            aggregatedResults.Add("Kaufland", kauflandResults);
            aggregatedResults.Add("Auchan", auchanResults);
            //aggregatedResults.Add("Profi", profiResults);
            aggregatedResults.Add("MegaImage", megaResults);

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

        public void QuitSelennium()
        {
            _seleniumService.Quit();
        }
    }
}
