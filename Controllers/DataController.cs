using System;
using System.IO;
using System.IO.Compression;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Data.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class DataController : Controller
    {
        private readonly IWebHostEnvironment _env;

        public DataController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpPost("upload")]
        public IActionResult Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Please select a zip file to upload.");

            if (!Path.GetExtension(file.FileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Only .zip files are allowed.");

            var uploadDir = Path.Combine(_env.ContentRootPath, "Uploads");
            Directory.CreateDirectory(uploadDir);

            var filePath = Path.Combine(uploadDir, file.FileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(fileStream);
            }

            return Ok($"File {file.FileName} uploaded successfully.");
        }

        [HttpGet("download/{fileName}")]
        public IActionResult Download(string fileName)
        {
            var uploadsFolder = Path.Combine(_env.ContentRootPath, "Uploads");
            var filePath = Path.Combine(uploadsFolder, fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            // Set the content type and file name
            var content = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var contentType = "application/octet-stream";
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            var fileExtension = Path.GetExtension(filePath);
            var downloadFileName = $"{fileNameWithoutExtension}{fileExtension}";

            return File(content, contentType, downloadFileName);
        }
    }
}
