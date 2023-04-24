// Import necessary namespaces
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;

// Define the namespace of the controller
namespace Data.Controllers
{
    // Define the ZipController class which inherits from the Controller class
    public class ZipController : Controller
    {
        // Define a HTTP GET action that returns the Index view
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // Define a HTTP POST action that accepts a URL and clones the website
        [HttpPost]
        public async Task<IActionResult> Index(string zipUrl)
        {
            // If the URL is empty or null, return a BadRequest response with an error message
            if (string.IsNullOrEmpty(zipUrl))
            {
                return BadRequest("Invalid input URL");
            }

            // Construct the base URL for the website
            var baseUrl = "https://" + zipUrl;

            // Define the path to the folder where the website will be downloaded
            var downloadFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "GotData");

            // Define the path to the zip file where the website will be stored
            var zipFilePath = Path.Combine(downloadFolderPath, $"{zipUrl}.zip");

            // If the download folder doesn't exist, create it
            if (!Directory.Exists(downloadFolderPath))
            {
                Directory.CreateDirectory(downloadFolderPath);
            }

            // Create a new ZipArchive to store the downloaded website
            using (var zipArchive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
            {
                // Call the DownloadAndZipPage method to download the website and add its contents to the ZipArchive
                await DownloadAndZipPage(baseUrl, zipArchive);
            }

            // Write a success message to the console
            Console.WriteLine($"Website cloned successfully to {zipFilePath}");

            // Return the Zip view with the path to the downloaded zip file
            return View("ZipComplete", new { zipFilePath });
        }

        // Define a private static method that downloads a webpage and adds its contents to a ZipArchive
        private static async Task DownloadAndZipPage(string url, ZipArchive zipArchive)
        {
            try
            {
                // Use an HTTP client to get the HTML content of the webpage
                var html = await new HttpClient().GetStringAsync(url);

                // Parse the HTML content using HtmlAgilityPack
                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);

                // Get the file path for the HTML file
                var htmlFilePath = GetHtmlFilePath(url);

                // Create a new ZipArchiveEntry for the HTML file and write its contents to the ZipArchive
                var entry = zipArchive.CreateEntry(htmlFilePath, CompressionLevel.Optimal);
                using (var stream = entry.Open())
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(html);
                }

                // Write a success message to the console
                Console.WriteLine($"Downloaded {url}");

                // Find all links on the webpage and download them recursively
                var linkNodes = htmlDocument.DocumentNode.Descendants("a");
                foreach (var linkNode in linkNodes)
                {
                    var href = linkNode.GetAttributeValue("href", "");
                    if (!string.IsNullOrEmpty(href) && !href.StartsWith("#"))
                    {
                        var absoluteUrl = new Uri(new Uri(url), href).AbsoluteUri;
                        await DownloadAndZipPage(absoluteUrl, zipArchive);
                    }
                }
            }
            // Catch any exceptions that occur during the download process and write an error message to the console
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading {url}: {ex.Message}");
            }
        }

        // This method receives a URL and returns the file path where the HTML content of the page will be saved in the zip file.
        private static string GetHtmlFilePath(string url)
        {
            // Creates a new Uri object based on the provided URL.
            var uri = new Uri(url);

            // Gets the file path by concatenating the host name and the absolute path of the URL.
            var filePath = uri.Host + uri.AbsolutePath;

            // If the file path ends with a forward slash, adds "index.html" to the end of the path.
            if (filePath.EndsWith("/"))
            {
                filePath += "index.html";
            }
            // If the file path does not have an extension, adds "/index.html" to the end of the path.
            else if (Path.GetExtension(filePath) == "")
            {
                filePath += "/index.html";
            }

            // Removes any leading forward slashes from the file path and returns it.
            return filePath.TrimStart('/');
        }
    }
}



