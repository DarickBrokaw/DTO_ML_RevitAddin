using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace GitHubReleaseChecker
{
    public class GitHubReleaseChecker
    {
        private readonly HttpClient _httpClient;

        public GitHubReleaseChecker()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AutoUpdater");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
        }

        public async Task<string> GetLatestVersionAsync(string repositoryOwner, string repositoryName)
        {
            var response = await _httpClient.GetAsync($"https://api.github.com/repos/{repositoryOwner}/{repositoryName}/releases/latest");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var release = JsonSerializer.Deserialize<GitHubRelease>(json);
                return release.TagName;
            }
            else
            {
                throw new Exception($"Error fetching latest release for repository '{repositoryOwner}/{repositoryName}'");
            }
        }

        private class GitHubRelease
        {
            public string TagName { get; set; }
        }
    }
}