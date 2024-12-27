using GithubScrapper.Models.ViewModels;
using GithubScrapper.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Octokit;
using System.Text;
using System.Threading.RateLimiting;

namespace GithubScrapper.Controllers
{
    [Authorize]
    public class GithubController : Controller
    {
        private readonly IGithubService _githubService;

        public GithubController(IGithubService githubService)
        {
            _githubService = githubService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> GetRateLimitStatus()
        {
            if(User.Identity.IsAuthenticated)
            {
                // GitHub Client Initialization
                var client = new GitHubClient(new ProductHeaderValue("GitHubToText"));
                var token = User.FindFirst("access_token")?.Value;

                if (!string.IsNullOrEmpty(token))
                {
                    client.Credentials = new Credentials(token); // Authenticate using the access token
                }

                // Fetch rate limits from GitHub
                var rateLimit = await client.RateLimit.GetRateLimits();

                // Return JSON with Core rate limit details
                return Json(new
                {
                    total = rateLimit.Resources.Core.Limit,
                    remaining = rateLimit.Resources.Core.Remaining,
                    resetTime = rateLimit.Resources.Core.Reset
                });
            }
            else
            {
                // Return JSON with Core rate limit details
                return Json(new
                {
                    total = 0,
                    remaining = 0,
                    resetTime = 0
                });
            }

        }



        [Authorize]
        public IActionResult ScrapeRepositories()
        {
            var model = new ScrapeRepositoriesViewModel
            {
                ValidExtensions = _githubService.GetValidExtensions().ToArray(),
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ExtractRepositories(string owner, string repoName, string[] fileExtensions, string fileType)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new
                {
                    Success = false,
                    Message = "User is not authenticated. Please log in and try again."
                });
            }

            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repoName) || fileExtensions == null || fileExtensions.Length == 0)
            {
                return Json(new
                {
                    Success = false,
                    Message = "All fields (Owner, repo name, file extensions) are required."
                });
            }

            try
            {
                // GitHub Client Initialization
                var client = new GitHubClient(new ProductHeaderValue("GitHubToText"));
                var token = User.FindFirst("access_token")?.Value;

                if (!string.IsNullOrEmpty(token))
                {
                    client.Credentials = new Credentials(token); // Authenticate using the access token

                    // Check the rate limits
                    var rateLimit = await client.Miscellaneous.GetRateLimits();

                    // Display the remaining requests and the reset time
                    Console.WriteLine($"Remaining requests: {rateLimit.Resources.Core.Remaining}");
                    Console.WriteLine($"Rate limit resets at: {rateLimit.Resources.Core.Reset}");
                }


                // Get the repository contents recursively
                var contents = await _githubService.GetRepositoryContentsRecursively(client, owner, repoName, fileExtensions.ToList());

                if (contents == null || contents.Count == 0)
                {
                    return Json(new
                    {
                        Success = false,
                        Message = "No files found with the specified owner, repo name and extensions."
                    });
                }

                string filePath = null;
                // Generate a unique file name for the output file
                if (fileType.Trim().ToLower() == "html".ToLower())
                {
                    filePath = await _githubService.GenerateRepositoryContentFileAsHtml(client, owner, repoName, contents);
                }
                else
                {
                    filePath = await _githubService.GenerateRepositoryContentFile(client, owner, repoName, contents);
                }


                string fileName = Path.GetFileName(filePath);

                return Json(new
                {
                    Success = true,
                    FileName = fileName,
                    Message = "Repositories scraped successfully! File is being downloaded."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Success = false,
                    Message = $"An error occurred: {ex.Message}"
                });
            }
        }

        public async Task<IActionResult> DownloadFileAsync(string fileName)
        {
            var client = new GitHubClient(new ProductHeaderValue("GitHubToText"));
            var token = User.FindFirst("access_token")?.Value;

            if (!string.IsNullOrEmpty(token))
            {
                client.Credentials = new Credentials(token); // Authenticate using the access token
            }
            var user = await client.User.Current();
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "AppFiles", "downloads", user.Login, fileName);

            // Check if the file exists
            if (!System.IO.File.Exists(filePath))
            {
                ViewData["FileName"] = fileName;
                return RedirectToAction("DownloadError");
            }

            // Serve the file for download
            var fileBytes = System.IO.File.ReadAllBytes(filePath);

            // Delete the file after serving it
            System.IO.File.Delete(filePath);

            // Return the file for download with the proper headers
            return File(fileBytes, "application/octet-stream", fileName);
        }

        public IActionResult DownloadSuccess()
        {
            return View();
        }
        public IActionResult DownloadError()
        {
            return View();
        }

        // Action to list all files in a specific folder
        [HttpGet]
        public async Task<IActionResult> ListFilesInFolderAsync()
        {
            try
            {
                var client = new GitHubClient(new ProductHeaderValue("GitHubToText"));
                var token = User.FindFirst("access_token")?.Value;

                if (!string.IsNullOrEmpty(token))
                {
                    client.Credentials = new Credentials(token); // Authenticate using the access token
                }
                var user = await client.User.Current();
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "AppFiles", "downloads", user.Login);
                // Create the directory if it does not exist
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // Get all the file names in the folder
                var fileNames = Directory.GetFiles(folderPath)
                                          .Select(file => Path.GetFileName(file))
                                          .ToList();

                // Return the list of file names in a JSON response
                return Json(new { success = true, files = fileNames });
            }
            catch (Exception ex)
            {
                // Handle errors
                return Json(new { success = false, message = ex.Message });
            }
        }

    }
}
