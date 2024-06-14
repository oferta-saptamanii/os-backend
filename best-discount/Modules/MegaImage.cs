using AngleSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace best_discount.Modules
{
    public class MegaImage
    {


        static Dictionary<string, string> megaCategories = new Dictionary<string, string>() {
            { "Animale de companie", "promotions:relevance:rootCategoryNameFacet:Animale+de+companie" },
            { "Apa si sucuri", "promotions:relevance:rootCategoryNameFacet:Apa+si+sucuri" },
            { "Bauturi", "promotions:relevance:rootCategoryNameFacet:Bauturi" },
            { "Cosmetice si ingrijire personala", "promotions:relevance:rootCategoryNameFacet:Cosmetice+si+ingrijire+personala" },
            { "Curatenie si nealimentare", "promotions:relevance:rootCategoryNameFacet:Curatenie+si+nealimentare" },
            { "Dulciuri si snacks", "promotions:relevance:rootCategoryNameFacet:Dulciuri+si+snacks" },
            { "Equilibrium", "promotions:relevance:rootCategoryNameFacet:Equilibrium" },
            { "Fructe si legume proaspete", "promotions:relevance:rootCategoryNameFacet:Fructe+si+legume+proaspete" },
            { "Ingrediente culinare", "promotions:relevance:rootCategoryNameFacet:Ingrediente+culinare" },
            { "Lactate si oua", "promotions:relevance:rootCategoryNameFacet:Lactate+si+oua" },
            { "Mama si ingrijire copil", "promotions:relevance:rootCategoryNameFacet:Mama+si+ingrijire+copil" },
            { "Mezeluri, carne si ready meal", "promotions:relevance:rootCategoryNameFacet:Mezeluri%2C+carne+si+ready+meal" },
            { "Paine, cafea, cereale si mic dejun", "promotions:relevance:rootCategoryNameFacet:Paine%2C+cafea%2C+cereale+si+mic+dejun" },
            { "Produse congelate", "promotions:relevance:rootCategoryNameFacet:Produse+congelate" },
            { "Produse sezoniere", "promotions:relevance:rootCategoryNameFacet:Produse+sezoniere" }
        };

        public static async Task<Dictionary<string, List<Product>>> ScrapeAsync()
        {
            Console.WriteLine("Scrapping MegaImage...");
            var pageData = new Dictionary<string, List<Product>>();

            using (HttpClient client = new HttpClient())
            {

                var config = Configuration.Default.WithDefaultLoader().WithXPath();
                var context = BrowsingContext.New(config);

                var url = "https://www.mega-image.ro/api/v1/";

                // Technically not needed at all :shrug:
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Host", "www.mega-image.ro");
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:127.0) Gecko/20100101 Firefox/127.0");
                client.DefaultRequestHeaders.Add("Referer", "https://www.mega-image.ro/search/promotii");
                client.DefaultRequestHeaders.Add("apollographql-client-name", "ro-mi-web-stores");
                client.DefaultRequestHeaders.Add("apollographql-client-version", "5c7b7237559c193d2b597dcd4a45d4d79c0dce96");
                client.DefaultRequestHeaders.Add("x-apollo-operation-name", "QlProductList");
                client.DefaultRequestHeaders.Add("x-apollo-operation-id", "165b8e6142299a8a626d2bdd850f956ee40e6be45bb9ecaacd0740558d322df4");
                client.DefaultRequestHeaders.Add("x-default-gql-refresh-token-disabled", "true");


                // Send POST
                foreach (var cat in megaCategories)
                {
                    try
                    {
                        var bodyContent = new
                        {
                            operationName = "QlProductList",
                            variables = new
                            {
                                productListingType = "PROMOTION_SEARCH",
                                lang = "ro",
                                productCodes = "",
                                categoryCode = "",
                                excludedProductCodes = "",
                                brands = "",
                                keywords = "",
                                productTypes = "",
                                numberOfItemsToDisplay = 500,
                                lazyLoadCount = 10,
                                pageNumber = 0,
                                sort = "",
                                searchQuery = cat.Value,
                                hideUnavailableProducts = true,
                                maxItemsToDisplay = 0
                            },
                            query = @"query QlProductList($productListingType: String!, $productListingCode: String, $lang: String, $sort: String, $searchQuery: String, $productCodes: String, $categoryCode: String, $excludedProductCodes: String, $brands: String, $keywords: String, $productTypes: String, $numberOfItemsToDisplay: Int, $lazyLoadCount: Int, $pageNumber: Int, $offerId: String, $hideUnavailableProducts: Boolean, $maxItemsToDisplay: Int, $currentCountProducts: Int) {
  qlProductList(
    productListingType: $productListingType
    productListingCode: $productListingCode
    lang: $lang
    sort: $sort
    searchQuery: $searchQuery
    productCodes: $productCodes
    categoryCode: $categoryCode
    excludedProductCodes: $excludedProductCodes
    brands: $brands
    keywords: $keywords
    productTypes: $productTypes
    numberOfItemsToDisplay: $numberOfItemsToDisplay
    lazyLoadCount: $lazyLoadCount
    pageNumber: $pageNumber
    offerId: $offerId
    hideUnavailableProducts: $hideUnavailableProducts
    maxItemsToDisplay: $maxItemsToDisplay
    currentCountProducts: $currentCountProducts
  ) {
    products {
      ...ProductBlockDetails
      __typename
    }
    breadcrumbs {
      facetCode
      facetName
      facetValueName
      facetValueCode
      removeQuery {
        query {
          value
          __typename
        }
        __typename
      }
      __typename
    }
    facets {
      code
      name
      category
      facetUiType
      values {
        code
        count
        name
        query {
          query {
            value
            __typename
          }
          __typename
        }
        selected
        __typename
      }
      __typename
    }
    sorts {
      name
      selected
      code
      __typename
    }
    pagination {
      currentPage
      totalResults
      totalPages
      sort
      __typename
    }
    freeTextSearch
    currentQuery {
      query {
        value
        __typename
      }
      __typename
    }
    __typename
  }
}

fragment ProductBlockDetails on Product {
  available
  averageRating
  numberOfReviews
  manufacturerName
  manufacturerSubBrandName
  code
  badges {
    ...ProductBadge
    __typename
  }
  badgeBrand {
    ...ProductBadge
    __typename
  }
  promoBadges {
    ...ProductBadge
    __typename
  }
  delivered
  littleLion
  freshnessDuration
  freshnessDurationTipFormatted
  frozen
  recyclable
  images {
    format
    imageType
    url
    __typename
  }
  isBundle
  isProductWithOnlineExclusivePromo
  maxOrderQuantity
  limitedAssortment
  mobileFees {
    ...MobileFee
    __typename
  }
  name
  newProduct
  onlineExclusive
  potentialPromotions {
    isMassFlashOffer
    endDate
    alternativePromotionMessage
    alternativePromotionBadge
    code
    priceToBurn
    promotionType
    pickAndMix
    qualifyingCount
    freeCount
    range
    redemptionLevel
    toDisplay
    description
    title
    promoBooster
    simplePromotionMessage
    offerType
    restrictionType
    priority
    percentageDiscount
    __typename
  }
  price {
    approximatePriceSymbol
    currencySymbol
    formattedValue
    priceType
    supplementaryPriceLabel1
    supplementaryPriceLabel2
    showStrikethroughPrice
    discountedPriceFormatted
    discountedUnitPriceFormatted
    unit
    unitPriceFormatted
    unitCode
    unitPrice
    value
    __typename
  }
  purchasable
  productPackagingQuantity
  productProposedPackaging
  productProposedPackaging2
  stock {
    inStock
    inStockBeforeMaxAdvanceOrderingDate
    partiallyInStock
    availableFromDate
    __typename
  }
  url
  previouslyBought
  nutriScoreLetter
  isLowPriceGuarantee
  isHouseholdBasket
  isPermanentPriceReduction
  freeGift
  plasticFee
  __typename
}

fragment ProductBadge on ProductBadge {
  code
  image {
    ...Image
    __typename
  }
  tooltipMessage
  name
  __typename
}

fragment Image on Image {
  altText
  format
  galleryIndex
  imageType
  url
  __typename
}

fragment MobileFee on MobileFee {
  feeName
  feeValue
  __typename
}"
                        };

                        var jsonBody = JsonConvert.SerializeObject(bodyContent);
                        var httpContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                        var response = await client.PostAsync(url, httpContent);

                        if (response.IsSuccessStatusCode)
                        {
                            var responseContent = await response.Content.ReadAsStringAsync();
                            var products = ProcessPage(responseContent, cat.Key);
                            pageData.Add(cat.Key, products);
                        }
                        else
                        {
                            Utils.Report($"failed to scrape for category '{cat.Key}'. Status code: {response.StatusCode}", Utils.ErrorType.ERROR);
                            // Utils.ReportError....
                        }
                    }
                    catch (Exception ex)
                    {
                        Utils.Report($"exception occured while scraping '{cat.Key}'. Exception: {ex.Message}", Utils.ErrorType.EXCEPTION);
                        // Utils.ReportError....
                    }
                }
            }
            return pageData;
        }

        private static List<Product> ProcessPage(string responseContent, string cat)
        {
            var products = new List<Product>();

            try
            {
                var jsonResponse = JObject.Parse(responseContent);
                var productList = jsonResponse?["data"]["qlProductList"]["products"];

                foreach (var product in productList)
                {

                    var productInfo = new Product
                    {
                        FullTitle = product?["name"].ToString(),
                        Image = "https://www.mega-image.ro" + product?["images"][0]["url"].ToString(),
                        Category = cat,
                        AvailableDate = product?["potentialPromotions"][0]["endDate"].ToString(),
                        DiscountPercentage = product?["potentialPromotions"][0]["simplePromotionMessage"].ToString(),
                        ProductUrl = "https://www.mega-image.ro" + product?["url"].ToString(),
                        CurrentPrice = product?["price"]["discountedPriceFormatted"].ToString(),
                        OriginalPrice = product?["price"]["formattedValue"].ToString()
                };

                    products.Add(productInfo);
                }
            }
            catch (Exception ex)
            {
                Utils.Report(ex.Message, Utils.ErrorType.EXCEPTION);
            }

            return products;
        }
    }
}
