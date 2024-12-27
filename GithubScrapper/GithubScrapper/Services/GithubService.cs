using GithubScrapper.Services;
using Microsoft.VisualBasic;
using Octokit;
using System.IO;
using System.Text;

public class GithubService : IGithubService
{

    // List of valid file extensions you want to process (can be customized)
    private List<string> validExtensions = new List<string> {
                ".cs",      // C#
                ".js",      // JavaScript
                ".java",    // Java
                ".py",      // Python
                ".html",    // HTML
                ".css",     // CSS
                ".cpp",     // C++
                ".h",       // C/C++ Header
                ".ts",      // TypeScript
                ".rb",      // Ruby
                ".go",      // Go
                ".json",    // JSON
                ".xml",     // XML
                ".yaml",    // YAML
                ".yml",     // YAML (alternative extension)
                ".md",      // Markdown
                ".txt",     // Plain text
                ".sql",     // SQL scripts
                ".sh",      // Shell scripts
                ".bat",     // Batch scripts
                ".ini",     // Configuration files
                ".env",     // Environment configuration
                ".php",     // PHP
                ".dart",    // Dart
                ".pl",      // Perl
                ".r",       // R
                ".swift",   // Swift
                ".scala",   // Scala
                ".kt",      // Kotlin
                ".rs",      // Rust
                ".erl",     // Erlang
                ".ex",      // Elixir
                ".exs",     // Elixir scripts
                ".cfg",     // Configuration files
                ".log",     // Log files
                ".gradle",  // Gradle build scripts
                ".properties", // Java properties files
                ".makefile",   // Makefiles
                ".dockerfile", // Dockerfiles
                ".tf",         // Terraform files
                ".toml",       // TOML configuration
                ".jsx",        // React JSX
                ".tsx",        // React TSX
                ".vue",        // Vue.js components
                ".vbs",        // VBScript
                ".asp",        // Classic ASP
                ".jsp",        // JavaServer Pages
                ".ejs",        // Embedded JavaScript templates
                ".handlebars", // Handlebars templates
                ".pug",        // Pug templates (formerly Jade)
    };

    // Method to get valid extensions
    public List<string> GetValidExtensions()
    {
        return validExtensions;
    }

    // Method to check if the file has a valid extension
    private bool IsCodeFile(string filePath, List<string> selectedExtensions)
    {
        // Get the file extension (case-insensitive)
        string fileExtension = Path.GetExtension(filePath)?.ToLower();

        // Check if the file extension matches one of the selected valid extensions
        return selectedExtensions.Contains(fileExtension);
    }

    public async Task<IReadOnlyList<RepositoryContent>> GetRepositoryContentsRecursively(
        GitHubClient client, string owner, string repoName, List<string> selectedExtensions, string path = null)
    {
        var rateLimit = await client.RateLimit.GetRateLimits();
        var contents = new List<RepositoryContent>();
        try
        {
            // Fetch rate limits from GitHub

            // Validate selected extensions
            var validExtensions = GetValidExtensions();
            selectedExtensions = selectedExtensions
                .Where(ext => validExtensions.Contains(ext.ToLower()))
                .ToList();

            if (!selectedExtensions.Any())
            {
                throw new ArgumentException("No valid extensions provided in the selected extensions.");
            }

            try
            {

                // Fetch the contents of the current directory or subdirectory
                IReadOnlyList<RepositoryContent> repoContents;
                if (string.IsNullOrEmpty(path))
                {
                    if(rateLimit.Resources.Core.Remaining > 0)
                    {
                        repoContents = await client.Repository.Content.GetAllContents(owner, repoName);
                    }
                    else
                    {
                        repoContents = new List<RepositoryContent>();
                    }
                }
                else
                {
                    if (rateLimit.Resources.Core.Remaining > 0)
                    {
                        repoContents = await client.Repository.Content.GetAllContents(owner, repoName, path);
                    }
                    else
                    {
                        repoContents = new List<RepositoryContent>();
                    }
                }

                Console.WriteLine($"Fetching contents for repository: {repoName}");
                Console.WriteLine($"Found {repoContents.Count} items in the repository root.");

                foreach (var content in repoContents)
                {
                    try
                    {
                        if (content.Type == Octokit.ContentType.Dir)
                        {
                            // If the content is a directory, recurse into it to fetch its contents
                            var subContents = await GetRepositoryContentsRecursively(
                                client, owner, repoName, selectedExtensions, content.Path);
                            contents.AddRange(subContents); // Add subdirectory contents
                        }
                        else if (IsCodeFile(content.Path, selectedExtensions)) // Ensure the file matches the selected extensions
                        {
                            contents.Add(content);
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching contents for repository {repoName}: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            contents = new List<RepositoryContent>();
        }


        return contents;
    }

    //public async Task<IReadOnlyList<RepositoryContent>> GetRepositoryContents(
    //GitHubClient client, string owner, string repoName, List<string> selectedExtensions)
    //{
    //    var contents = new List<RepositoryContent>();
    //    var validExtensions = GetValidExtensions();

    //    // Filter valid extensions
    //    selectedExtensions = selectedExtensions
    //        .Where(ext => validExtensions.Contains(ext.ToLower()))
    //        .ToList();

    //    if (!selectedExtensions.Any())
    //    {
    //        throw new ArgumentException("No valid extensions provided in the selected extensions.");
    //    }

    //    try
    //    {
    //        // Fetch the repository tree
    //        var tree = await client.Repository.Content.GetAllContentsByRef(owner, repoName, "master");

    //        // Filter files based on extensions
    //        contents = tree
    //            .Where(content => content.Type == Octokit.ContentType.File && IsCodeFile(content.Path, selectedExtensions))
    //            .ToList();
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"Error fetching repository contents: {ex.Message}");
    //    }

    //    return contents;
    //}

    public async Task AppendContentToFile(GitHubClient client, string owner, string repoName, RepositoryContent content, string filePath)
    {
        var rateLimit = await client.RateLimit.GetRateLimits();

        try
        {
            byte[] fileContentBytes = new byte[0];

            // Check if we have remaining API calls
            if (rateLimit.Resources.Core.Remaining > 0)
            {
                // Fetch the raw content of the file
                fileContentBytes = await client.Repository.Content.GetRawContent(owner, repoName, content.Path);
            }
            else
            {
                fileContentBytes = new byte[0]; // Empty array if no remaining API calls
            }

            // Convert the byte array to string (assuming UTF-8 encoding)
            string fileContent = Encoding.UTF8.GetString(fileContentBytes);

            // Append the file content to the existing file
            await using (var writer = new StreamWriter(filePath, append: true))
            {
                writer.WriteLine($"--- File: {content.Path} ---");
                writer.WriteLine(fileContent);
                writer.WriteLine("\n\n");
            }
        }
        catch (Exception ex)
        {
            // Handle any errors by appending an error message to the file
            await using (var writer = new StreamWriter(filePath, append: true))
            {
                writer.WriteLine($"--- File: {content.Path} ---");
                writer.WriteLine($"Error fetching file content: {ex.Message}");
                writer.WriteLine("\n\n");
            }
        }
    }


    public async Task<string> GenerateRepositoryContentFile(GitHubClient client, string owner, string repoName, IReadOnlyList<RepositoryContent> contents)
    {
        var rateLimit = await client.RateLimit.GetRateLimits();

        var user = await client.User.Current();
        Console.WriteLine($"Authenticated as: {user.Login}");

        string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"); // UTC timestamp with milliseconds
        string fileName = $"Contents-{owner}-{repoName}.txt";
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "AppFiles", "downloads", user.Login, fileName);

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        await using (var writer = new StreamWriter(filePath))
        {
            foreach (var content in contents)
            {
                if (content.Type == ContentType.File)
                {
                    try
                    {
                        byte[] fileContentBytes = new byte[0];
                        if (rateLimit.Resources.Core.Remaining > 0)
                        {
                            fileContentBytes = await client.Repository.Content.GetRawContent(owner, repoName, content.Path);
                        }
                        else
                        {
                            fileContentBytes = new byte[0];
                        }


                        string fileContent = Encoding.UTF8.GetString(fileContentBytes);
                        writer.WriteLine($"--- File: {content.Path} ---");
                        writer.WriteLine(fileContent);
                        writer.WriteLine("\n\n");

                    }
                    catch (Exception ex)
                    {
                        writer.WriteLine($"--- File: {content.Path} ---");
                        writer.WriteLine($"Error fetching file content: {ex.Message}");
                        writer.WriteLine("\n\n");
                    }
                }

                //// Check API call limit before continuing
                //if (totalApiCalls >= MaxApiCalls)
                //{
                //    throw new InvalidOperationException("API call limit exceeded.");
                //}
            }
        }

        return filePath;
    }

    public async Task AppendContentToHtmlFile(GitHubClient client, string owner, string repoName, RepositoryContent content, string filePath)
    {
        var rateLimit = await client.RateLimit.GetRateLimits();

        try
        {
            byte[] fileContentBytes = new byte[0];

            // Check if we have remaining API calls
            if (rateLimit.Resources.Core.Remaining > 0)
            {
                // Fetch the raw content of the file
                fileContentBytes = await client.Repository.Content.GetRawContent(owner, repoName, content.Path);
            }
            else
            {
                fileContentBytes = new byte[0]; // Empty array if no remaining API calls
            }

            // Convert the byte array to string (assuming UTF-8 encoding)
            string fileContent = Encoding.UTF8.GetString(fileContentBytes);

            // Append the HTML content for the file entry
            StringBuilder htmlContent = new StringBuilder();
            htmlContent.AppendLine($"<div class='file-entry'>");
            htmlContent.AppendLine($"<div class='file-name' onclick='toggleContent(\"{content.Path}\")'>{content.Path}</div>");
            htmlContent.AppendLine($"<div id='{content.Path}' class='file-content'>{fileContent}</div>");
            htmlContent.AppendLine($"</div>");

            // Append to the existing HTML file
            await using (var writer = new StreamWriter(filePath, append: true))
            {
                await writer.WriteLineAsync(htmlContent.ToString());
            }
        }
        catch (Exception ex)
        {
            // Handle any errors by appending an error message to the HTML file
            StringBuilder errorContent = new StringBuilder();
            errorContent.AppendLine($"<div class='file-entry'>");
            errorContent.AppendLine($"<div class='file-name'>{content.Path}</div>");
            errorContent.AppendLine($"<div class='file-content'>Error fetching file content: {ex.Message}</div>");
            errorContent.AppendLine($"</div>");

            // Append error to the existing HTML file
            await using (var writer = new StreamWriter(filePath, append: true))
            {
                await writer.WriteLineAsync(errorContent.ToString());
            }
        }
    }


    public async Task<string> GenerateRepositoryContentFileAsHtml(GitHubClient client, string owner, string repoName, IReadOnlyList<RepositoryContent> contents)
    {
        var rateLimit = await client.RateLimit.GetRateLimits();

        var user = await client.User.Current();
        Console.WriteLine($"Authenticated as: {user.Login}");

        string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"); // UTC timestamp with milliseconds
        string fileName = $"Contents-{owner}-{repoName}.html";
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "AppFiles", "downloads", user.Login, fileName);

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        StringBuilder htmlContent = new StringBuilder();
        htmlContent.AppendLine("<html>");
        htmlContent.AppendLine("<head>");
        htmlContent.AppendLine("<title>Repository Files</title>");
        htmlContent.AppendLine("<style>");
        htmlContent.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        htmlContent.AppendLine("h2 { color: #333; }");
        htmlContent.AppendLine(".file-name { cursor: pointer; color: #0066cc; text-decoration: underline; }");
        htmlContent.AppendLine(".file-content { display: none; margin-top: 10px; white-space: pre-wrap; background-color: #f4f4f4; padding: 10px; border: 1px solid #ccc; }");
        htmlContent.AppendLine(".copy-button { margin-top: 5px; background-color: #007bff; color: #fff; border: none; padding: 5px 10px; cursor: pointer; }");
        htmlContent.AppendLine(".search-container { margin-bottom: 20px; }");
        htmlContent.AppendLine(".search-container input { padding: 8px; width: 300px; }");
        htmlContent.AppendLine("</style>");
        htmlContent.AppendLine("<script>");
        htmlContent.AppendLine("function filterFiles() {");
        htmlContent.AppendLine("  var searchQuery = document.getElementById('searchBar').value.toLowerCase();");
        htmlContent.AppendLine("  var fileEntries = document.getElementsByClassName('file-entry');");
        htmlContent.AppendLine("  for (var i = 0; i < fileEntries.length; i++) {");
        htmlContent.AppendLine("    var fileName = fileEntries[i].getElementsByClassName('file-name')[0].innerText.toLowerCase();");
        htmlContent.AppendLine("    if (fileName.indexOf(searchQuery) > -1) {");
        htmlContent.AppendLine("      fileEntries[i].style.display = '';"); // Show matching file
        htmlContent.AppendLine("    } else {");
        htmlContent.AppendLine("      fileEntries[i].style.display = 'none';"); // Hide non-matching file
        htmlContent.AppendLine("    }");
        htmlContent.AppendLine("  }");
        htmlContent.AppendLine("}");
        htmlContent.AppendLine("function toggleContent(contentId) {");
        htmlContent.AppendLine("  var contentDiv = document.getElementById(contentId);");
        htmlContent.AppendLine("  if (contentDiv.style.display === 'none' || contentDiv.style.display === '') {");
        htmlContent.AppendLine("    contentDiv.style.display = 'block';");
        htmlContent.AppendLine("  } else {");
        htmlContent.AppendLine("    contentDiv.style.display = 'none';");
        htmlContent.AppendLine("  }");
        htmlContent.AppendLine("}");
        htmlContent.AppendLine("function copyToClipboard(contentId) {");
        htmlContent.AppendLine("  var contentDiv = document.getElementById(contentId);");
        htmlContent.AppendLine("  var text = contentDiv.innerText;");
        htmlContent.AppendLine("  navigator.clipboard.writeText(text).then(function() {");
        htmlContent.AppendLine("    alert('Content copied to clipboard!');");
        htmlContent.AppendLine("  }, function(err) {");
        htmlContent.AppendLine("    alert('Failed to copy: ' + err);");
        htmlContent.AppendLine("  });");
        htmlContent.AppendLine("}");
        htmlContent.AppendLine("</script>");
        htmlContent.AppendLine("</head>");
        htmlContent.AppendLine("<body>");

        htmlContent.AppendLine("<div class='search-container'>");
        htmlContent.AppendLine("<input type='text' id='searchBar' placeholder='Search for a file...' onkeyup='filterFiles()'>");
        htmlContent.AppendLine("</div>");

        htmlContent.AppendLine("<h2>Repository Files</h2>");
        htmlContent.AppendLine("<div id='fileList'>");

        foreach (var content in contents)
        {
            if (content.Type == ContentType.File)
            {
                try
                {
                    byte[] fileContentBytes = await client.Repository.Content.GetRawContent(owner, repoName, content.Path);

                    string fileContent = Encoding.UTF8.GetString(fileContentBytes);

                    htmlContent.AppendLine($"<div class='file-entry'>");
                    htmlContent.AppendLine($"<div class='file-name' onclick='toggleContent(\"{content.Path}\")'>{content.Path}</div>");
                    htmlContent.AppendLine($"<div id='{content.Path}' class='file-content'>{fileContent}</div>");
                    htmlContent.AppendLine($"<button class='copy-button' onclick='copyToClipboard(\"{content.Path}\")'>Copy</button>");
                    htmlContent.AppendLine($"</div>");
                }
                catch (Exception ex)
                {
                    htmlContent.AppendLine($"<div class='file-entry'>");
                    htmlContent.AppendLine($"<div class='file-name'>{content.Path}</div>");
                    htmlContent.AppendLine($"<div class='file-content'>Error fetching file content: {ex.Message}</div>");
                    htmlContent.AppendLine($"</div>");
                }
            }
        }

        htmlContent.AppendLine("</div>");
        htmlContent.AppendLine("</body>");
        htmlContent.AppendLine("</html>");

        await System.IO.File.WriteAllTextAsync(filePath, htmlContent.ToString());

        return filePath;
    }


}
