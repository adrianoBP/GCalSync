using Newtonsoft.Json;
using RestSharp;
using System;

namespace GCalSync.Helpers
{
    public class APIHelper
    {
        public static T MakeRequest<T>(string URL, Method method, object content = null)
        {
            var client = new RestClient($"{URL}");
            var request = new RestRequest(method)
                .AddHeader("Content-Type", "application/json");

            if (content != null)
                request.AddJsonBody(JsonConvert.SerializeObject(content));

            var response = client.Execute(request);

            if (!response.IsSuccessful)
                throw new Exception(string.IsNullOrWhiteSpace(response.ErrorException?.Message) ? $"No error message provided - {response.Content}" : response.ErrorException.Message);

            if (string.IsNullOrWhiteSpace(response.Content))
                return default;

            try
            {
                return JsonConvert.DeserializeObject<T>(response.Content);
            }
            catch (Exception)
            {
                throw new Exception($"Unable to deserialize");
            }
        }
    }
}
