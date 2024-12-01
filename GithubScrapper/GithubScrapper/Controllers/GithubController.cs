using GithubScrapper.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GithubScrapper.Controllers
{
    [Authorize]
    public class GithubController : Controller
    {
        // List of file extensions
        public string[] ValidExtensions = {
        ".cs", ".js", ".java", ".py", ".html", ".css", ".cpp", ".h", ".ts", ".rb", ".go", ".json", ".xml", ".yaml", ".yml", ".md", ".txt", ".sql", ".sh", ".bat", ".ini", ".env", ".php", ".dart", ".pl", ".r", ".swift", ".scala", ".kt", ".rs", ".erl", ".ex", ".exs", ".cfg", ".log", ".gradle", ".properties", ".makefile", ".dockerfile", ".tf", ".toml", ".jsx", ".tsx", ".vue", ".vbs", ".asp", ".jsp", ".ejs", ".handlebars", ".pug"
        };
        [Authorize]
        public IActionResult ScrapeRepositories()
        {
            var model = new ScrapeRepositoriesViewModel
            {
                ValidExtensions = ValidExtensions
            };

            return View(model);
        }


        [HttpPost]
        [Authorize]
        public IActionResult ExtractRepositories(string owner, string repoName, string[] fileExtensions)
        {
            // Check if the user is authenticated
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repoName) || fileExtensions == null || fileExtensions.Length == 0)
            {
                return BadRequest("All fields are required.");
            }

            // For demonstration, just returning the received data
            var response = new
            {
                Owner = owner,
                RepoName = repoName,
                FileExtensions = fileExtensions,
                Message = "Repositories scraped successfully!"
            };

            return Json(response);
        }
    }
}
