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
    }
}
