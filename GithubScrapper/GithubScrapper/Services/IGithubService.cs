using Octokit;

namespace GithubScrapper.Services
{
    public interface IGithubService
    {
        public Task<IReadOnlyList<RepositoryContent>> GetRepositoryContentsRecursively(GitHubClient client, string owner, string repoName, List<string> selectedExtensions, string path = null);

        Task<string> GenerateRepositoryContentFile(GitHubClient client, string owner, string repoName, IReadOnlyList<RepositoryContent> contents);
        Task<string> GenerateRepositoryContentFileAsHtml(GitHubClient client, string owner, string repoName, IReadOnlyList<RepositoryContent> contents);
        List<string> GetValidExtensions();
    }
}
