using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
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
        public async Task<IActionResult> Index(string zipUrl)
        {
            if (string.IsNullOrEmpty(zipUrl))
            {
                return BadRequest("Invalid input URL");
            }

            var baseUrl = "https://" + zipUrl;

            var downloadFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "GotData");

            if (!Directory.Exists(downloadFolderPath))
            {
                Directory.CreateDirectory(downloadFolderPath);
            }
            var zipFilePath = Path.Combine(downloadFolderPath, $"{zipUrl}.zip");

            if (System.IO.File.Exists(zipFilePath))
            {
                var randomNumber = new Random().Next();
                zipFilePath = Path.Combine(downloadFolderPath, $"{zipUrl}{randomNumber}.zip");

            }

            using (var zipArchive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
            {
                await DownloadAndZipPage(baseUrl, zipArchive);
            }

            Console.WriteLine($"Website cloned successfully to {zipFilePath}");

            ViewBag.zipFilePath = zipFilePath;
            ViewBag.baseUrl = baseUrl;
            ViewBag.downloadFolderPath = downloadFolderPath;
            ViewBag.ziplocation = $"/Data/GotData/{zipUrl}.zip";


            return View("ZipComplete", new { zipFilePath });
        }

//         [HttpGet("/download/one")] 
//         public IActionResult Download()
//         {
//             // // Specify the path and filename of the file you want to download
//             // var filePath = ViewBag.ZipFilePath; // replace with the actual path to your file
//             // var fileName = Path.GetFileName(filePath); // replace with the actual name of your file

//             // // Get a reference to the current HTTP response object
//             // var response = HttpContext.Response;

//             // // Clear the response object and set its content type to "application/octet-stream", which is a generic binary file type
//             // response.Clear();
//             // response.ContentType = "application/octet-stream";

//             // // Add a content disposition header to the response, specifying that the file should be downloaded as an attachment with the specified filename
//             // response.Headers.Add("Content-Disposition", "attachment; filename=\"" + fileName + "\"");

//             // // Write the contents of the file to the response object using the WriteFile method
//             // response.WriteFile(filePath);

//             // // End the response to complete the download
//             // response.CompleteAsync();

//             // return RedirectToAction("Index");
//     // Specify the path and filename of the file you want to download

//     string filePath = ViewBag.ziplocation; // replace with the actual path to your file
//     string fileName = "site.css"; // replace with the actual name of your file

//     // Return a FileStreamResult that streams the file contents to the response
//     return File(new FileStream(filePath, FileMode.Open), "application/octet-stream", fileName);
// }
        

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

                var scriptNodes = htmlDocument.DocumentNode.Descendants("script");
                foreach (var scriptNode in scriptNodes)
                {
                    var src = scriptNode.GetAttributeValue("src", "");
                    if (!string.IsNullOrEmpty(src))
                    {
                        var absoluteUrl = new Uri(new Uri(url), src).AbsoluteUri;
                        await DownloadAndZipFile(absoluteUrl, zipArchive);
                    }
                }

                var linkNodesWithCss = htmlDocument.DocumentNode.Descendants("link").Where(x => x.GetAttributeValue("rel", "").Contains("stylesheet"));
                foreach (var linkNode in linkNodesWithCss)
                {
                    var href = linkNode.GetAttributeValue("href", "");
                    if (!string.IsNullOrEmpty(href))
                    {
                        var absoluteUrl = new Uri(new Uri(url), href).AbsoluteUri;
                        await DownloadAndZipFile(absoluteUrl, zipArchive);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading {url}: {ex.Message}");
            }
        }

        private static async Task DownloadAndZipFile(string url, ZipArchive zipArchive)
        {
            try
            {
                var httpClient = new HttpClient();
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);

                var response = await httpClient.SendAsync(requestMessage);

                if (response.IsSuccessStatusCode)
                {
                    var fileContent = await response.Content.ReadAsByteArrayAsync();
                    var fileName = Path.GetFileName(url);

                    if (IsHtmlFile(url))
                    {
                        fileName = GetHtmlFilePath(url);
                    }

                    var entry = zipArchive.CreateEntry(fileName, CompressionLevel.Optimal);
                    using (var stream = entry.Open())
                    using (var writer = new BinaryWriter(stream))
                    {
                        writer.Write(fileContent);
                    }

                    Console.WriteLine($"Downloaded {url}");

                    if (IsHtmlFile(url))
                    {
                        var htmlDocument = new HtmlDocument();
                        htmlDocument.LoadHtml(Encoding.UTF8.GetString(fileContent));

                        var linkNodes = htmlDocument.DocumentNode.Descendants("link");
                        foreach (var linkNode in linkNodes)
                        {
                            var href = linkNode.GetAttributeValue("href", "");
                            if (!string.IsNullOrEmpty(href) && !href.StartsWith("#") && IsCssFile(href))
                            {
                                var absoluteUrl = new Uri(new Uri(url), href).AbsoluteUri;
                                await DownloadAndZipFile(absoluteUrl, zipArchive);
                            }
                        }

                        var scriptNodes = htmlDocument.DocumentNode.Descendants("script");
                        foreach (var scriptNode in scriptNodes)
                        {
                            var src = scriptNode.GetAttributeValue("src", "");
                            if (!string.IsNullOrEmpty(src) && !src.StartsWith("#") && IsJavaScriptFile(src))
                            {
                                var absoluteUrl = new Uri(new Uri(url), src).AbsoluteUri;
                                await DownloadAndZipFile(absoluteUrl, zipArchive);
                            }
                        }

                        var iframeNodes = htmlDocument.DocumentNode.Descendants("iframe");
                        foreach (var iframeNode in iframeNodes)
                        {
                            var src = iframeNode.GetAttributeValue("src", "");
                            if (!string.IsNullOrEmpty(src) && !src.StartsWith("#"))
                            {
                                var absoluteUrl = new Uri(new Uri(url), src).AbsoluteUri;
                                await DownloadAndZipFile(absoluteUrl, zipArchive);
                            }
                        }
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

        private static bool IsHtmlFile(string url)
        {
            var extension = Path.GetExtension(url);
            return string.Equals(extension, ".html", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".htm", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsCssFile(string url)
        {
            var extension = Path.GetExtension(url);
            return string.Equals(extension, ".css", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsJavaScriptFile(string url)
        {
            var extension = Path.GetExtension(url);
            return string.Equals(extension, ".js", StringComparison.OrdinalIgnoreCase);
        }
    }
    
}

