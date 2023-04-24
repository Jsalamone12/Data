
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;

namespace Data.Controllers
{
    public class ZipController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string inputUrl, string zipUrl)
        {
            var baseUrl = "https://" + inputUrl;
            var downloadFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "GotData");
            var zipFilePath = Path.Combine(downloadFolderPath, $"{zipUrl}.zip");

            if (!Directory.Exists(downloadFolderPath))
            {
                Directory.CreateDirectory(downloadFolderPath);
            }

            using (var zipArchive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
            {
                await DownloadAndZipPage(baseUrl, zipArchive);
            }

            Console.WriteLine($"Website cloned successfully to {zipFilePath}");

            return View("Zip", new { zipFilePath });
        }

        private static async Task DownloadAndZipPage(string url, ZipArchive zipArchive)
        {
            try
            {
                var html = await new HttpClient().GetStringAsync(url);
                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);

                var htmlFilePath = GetHtmlFilePath(url);
                var entry = zipArchive.CreateEntry(htmlFilePath, CompressionLevel.Optimal);
                using (var stream = entry.Open())
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(html);
                }

                Console.WriteLine($"Downloaded {url}");

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
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading {url}: {ex.Message}");
            }
        }

        private static string GetHtmlFilePath(string url)
        {
            var uri = new Uri(url);
            var filePath = uri.Host + uri.AbsolutePath;
            if (filePath.EndsWith("/"))
            {
                filePath += "index.html";
            }
            else if (Path.GetExtension(filePath) == "")
            {
                filePath += "/index.html";
            }
            return filePath.TrimStart('/');
        }
    }
}
