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
    }
}
