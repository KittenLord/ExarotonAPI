using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Exaroton.Internal
{
    
    internal class APIClient // possibly make this not static? it will be difficult to manage tho
    {
        private static HttpClient _client = new HttpClient();
        private static string _token { get; set; } = "";


        private static async Task<Response> SendMessageAsync(HttpMethod method, string url, string content, bool octetStream = false)
        {
            if(_token == "" || _token is null) throw new Exception();

            

            HttpRequestMessage hq = new HttpRequestMessage(method, new Uri(url));
            var contentType = "application/json";
            if(octetStream) contentType = "application/octet-stream";

            hq.Content = new StringContent(content, new MediaTypeHeaderValue(contentType));    
            hq.Headers.Add("Authorization", "Bearer " + _token);

            var httpResponse = await _client.SendAsync(hq).ConfigureAwait(false);
            var responseContent = await httpResponse.Content.ReadAsStringAsync();
            var response = JsonConvert.DeserializeObject<Response>(responseContent, new JsonConverterObjectToString());

            if(response is null) throw new Exception();

            if(!response.IsSuccess) throw new Exception(response.Error);

            return response;
        }
        private static async Task<Response> SendMessageAsync(HttpMethod method, string url, bool octetStream = false)
        {
            return await SendMessageAsync(method, url, "", octetStream);
        }

        public static async Task<Response> GetRequestAsync(string url)
        {
            return await SendMessageAsync(HttpMethod.Get, url);
        }

        public static async Task<Response> PostRequestAsync(string url, string content)
        {
            return await SendMessageAsync(HttpMethod.Post, url, content);
        }

        // octetStream is for file writing?
        public static async Task<Response> PutRequestAsync(string url, string content, bool octetStream = false)
        {
            return await SendMessageAsync(HttpMethod.Put, url, content, octetStream);
        }

        public static async Task<Response> DeleteRequestAsync(string url, string content)
        {
            return await SendMessageAsync(HttpMethod.Delete, url, content);
        }

        public static async Task<Response> DeleteRequestAsync(string url)
        {
            return await DeleteRequestAsync(url, "");
        }



        public static void SetToken(string token)
        {
            if(token == "") throw new Exception();
            _token = token;
        }
    }

    class Response
    {
        [JsonProperty("success")]
        public bool IsSuccess { get; private set; }
        [JsonProperty("error")]
        public string Error { get; private set; }
        [JsonProperty("data")]
        public string Data { get; private set; }

        public T BuildData<T>()
        {
            if(!IsSuccess) throw new Exception(Error);

            var data = JsonConvert.DeserializeObject<T>(Data);

            if(data is null) throw new Exception();

            return data;
        }

        [JsonConstructor]
        private Response(bool isSuccess, string error, string data)
        {
            IsSuccess = isSuccess;
            Error = error;
            Data = data;
        }
    }
}