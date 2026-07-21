using AIForOrcas.DTO.API.Tags;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace AIForOrcas.Client.BL.Services
{
    public class TagService : ITagService
    {
		private readonly IHttpClientFactory _httpClientFactory;
		private string api = "api/tags";
		private JsonSerializerOptions defaultJsonSerializerOptions => new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };

		public TagService(IHttpClientFactory httpClientFactory)
		{
			_httpClientFactory = httpClientFactory;
		}

		// Get the list of unique tags
		public async Task<List<string>> GetUniqueTagsAsync()
		{
			var httpClient = _httpClientFactory.CreateClient("UnauthenticatedAPI");
			var httpResponseMessage = await httpClient.GetAsync(api);

			if (httpResponseMessage.IsSuccessStatusCode)
			{
				var responseString = await httpResponseMessage.Content.ReadAsStringAsync();

				if (string.IsNullOrWhiteSpace(responseString))
					return new List<string>();

				return JsonSerializer.Deserialize<List<string>>(responseString, defaultJsonSerializerOptions);
			}
			else
			{
				return new List<string>();
			}
		}

		// replace the current tag with a new one
		public async Task<int> UpdateTagAsync(TagUpdate payload)
		{
			var dataJson = JsonSerializer.Serialize(payload);
			var stringContent = new StringContent(dataJson, Encoding.UTF8, "application/json");

			var httpClient = _httpClientFactory.CreateClient("AuthenticatedAPI");
			var httpResponseMessage = await httpClient.PutAsync(api, stringContent);

			if (httpResponseMessage.IsSuccessStatusCode)
			{
				var responseString = await httpResponseMessage.Content.ReadAsStringAsync();

				if (string.IsNullOrWhiteSpace(responseString))
					return 0;

				return JsonSerializer.Deserialize<int>(responseString, defaultJsonSerializerOptions);
			}
			else
			{
				return 0;
			}
		}

		// delete a tag completely from the database
		public async Task<int> DeleteTagAsync(string tag)
		{
			var url = $"{api}?tag={HttpUtility.UrlEncode(tag)}";

			var httpClient = _httpClientFactory.CreateClient("AuthenticatedAPI");
			var httpResponseMessage = await httpClient.DeleteAsync(url);

			if (httpResponseMessage.IsSuccessStatusCode)
			{
				var responseString = await httpResponseMessage.Content.ReadAsStringAsync();

				if (string.IsNullOrWhiteSpace(responseString))
					return 0;

				return JsonSerializer.Deserialize<int>(responseString, defaultJsonSerializerOptions);
			}
			else
			{
				return 0;
			}
		}

	}
}
