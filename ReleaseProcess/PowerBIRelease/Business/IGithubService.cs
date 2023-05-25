using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace PowerBIRelease.Business
{
    public interface IGitHubService
    {
        Task<GitHubReleaseDTO> GetRelease(String tag,
                                          CancellationToken cancellationToken);

        Task<String> GetReleaseAsset(Int32 assetId,
                                     String assetName,
                                     String outputPath,
                                     CancellationToken cancellationToken);
    }

    public class GitHubService : IGitHubService{
        private readonly String Token;

        private readonly HttpClient HttpClient;

        private readonly String GithubApiUrl;

        public GitHubService(String githubApiUrl, String token){
            this.GithubApiUrl = githubApiUrl;
            this.Token = token;
            this.HttpClient = new HttpClient();
        }

        public async Task<GitHubReleaseDTO> GetRelease(String tag, CancellationToken cancellationToken)
        {
            String requestUri = $"{this.GithubApiUrl}/releases/tags/{tag}";
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            requestMessage.Headers.UserAgent.Add(new ProductInfoHeaderValue("PostmanRuntime", "7.28.0"));

            var responseMessage = await this.HttpClient.SendAsync(requestMessage, cancellationToken);

            if (responseMessage.IsSuccessStatusCode == false)
            {
                throw new Exception($"Unable to find release with tag {tag}");
            }

            var content = await responseMessage.Content.ReadAsStringAsync(cancellationToken);

            // Get the import id from the import response
            GitHubReleaseDTO responseDto = JsonConvert.DeserializeObject<GitHubReleaseDTO>(content);

            return responseDto;
        }

        public async Task<String> GetReleaseAsset(Int32 assetId,
                                                  String assetName,
                                                  String outputPath,
                                                  CancellationToken cancellationToken)
        {
            String requestUri = $"{this.GithubApiUrl}/releases/assets/{assetId}";
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
            requestMessage.Headers.UserAgent.Add(new ProductInfoHeaderValue("PostmanRuntime", "7.28.0"));

            var responseMessage = await this.HttpClient.SendAsync(requestMessage, cancellationToken);

            if (responseMessage.IsSuccessStatusCode == false)
            {
                throw new Exception($"Unable to find release asset with id {assetId}");
            }

            var content = await responseMessage.Content.ReadAsStreamAsync(cancellationToken);

            using (var fileStream = File.Create($"{outputPath}\\{assetName}"))
            {
                content.Seek(0, SeekOrigin.Begin);
                content.CopyTo(fileStream);
            }

            return $"{outputPath}\\{assetName}";
        }
    }

    public class GitHubReleaseDTO
    {
        [JsonProperty("id")]
        public Int32 Id { get; set; }

        [JsonProperty("body")]
        public String Body { get; set; }

        [JsonProperty("tag_name")]
        public String Tag { get; set; }

        [JsonProperty("assets")]
        public List<GithubReleaseAsset> Assets { get; set; }
    }

    public class GithubReleaseAsset
    {
        [JsonProperty("id")]
        public Int32 Id { get; set; }

        [JsonProperty("name")]
        public String Name { get; set; }
    }
}
