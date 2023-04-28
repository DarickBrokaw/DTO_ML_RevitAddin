using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public async Task<GitHubRelease> GetLatestVersionAsync(string repositoryOwner, string repositoryName)
        {

            var response = await _httpClient.GetAsync($"https://api.github.com/repos/{repositoryOwner}/{repositoryName}/releases/latest");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStreamAsync();
                var serializer = new DataContractJsonSerializer(typeof(GitHubRelease));
                var release = (GitHubRelease)serializer.ReadObject(json);
                return release;
            }
            else
            {
                throw new Exception($"Error fetching latest release for repository '{repositoryOwner}/{repositoryName}'");
            }
        }

        public async Task DownloadReleaseAssetsAsync(GitHubRelease release, string destinationFolder, List<string> assetFileNamesToDownload = null)
        {
            if (!Directory.Exists(destinationFolder))
            {
                Directory.CreateDirectory(destinationFolder);
            }

            var assetsToDownload = assetFileNamesToDownload == null ? release.Assets : release.Assets.Where(asset => assetFileNamesToDownload.Contains(asset.Name)).ToList();

            foreach (var asset in assetsToDownload)
            {
                string destinationFilePath = Path.Combine(destinationFolder, asset.Name);
                await DownloadFileAsync(asset.DownloadUrl, destinationFilePath);
            }
        }

        private async Task DownloadFileAsync(string downloadUrl, string destinationFilePath)
        {
            using (var response = await _httpClient.GetAsync(downloadUrl))
            {
                using (var fileStream = File.Create(destinationFilePath))
                {
                    await response.Content.CopyToAsync(fileStream);
                }
            }
        }


        [DataContract]
        public class GitHubRelease
        {
            [DataMember(Name = "tag_name")]
            public string TagName { get; set; }

            [DataMember(Name = "assets")]
            public List<GitHubAsset> Assets { get; set; }
        }

        [DataContract]
        public class GitHubAsset
        {
            [DataMember(Name = "name")]
            public string Name { get; set; }

            [DataMember(Name = "browser_download_url")]
            public string DownloadUrl { get; set; }
        }
    }
}