using System;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GitHubConnect
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
                var json = await response.Content.ReadAsStreamAsync();
                var serializer = new DataContractJsonSerializer(typeof(GitHubRelease));
                var release = (GitHubRelease)serializer.ReadObject(json);
                return release.TagName;
            }
            else
            {
                throw new Exception($"Error fetching latest release for repository '{repositoryOwner}/{repositoryName}'");
            }
        }
        [DataContract]
        private class GitHubRelease
        {
            [DataMember(Name = "tag_name")]
            public string TagName { get; set; }
        }
    }
}