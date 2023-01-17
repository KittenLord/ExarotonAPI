using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Exaroton.Internal;

namespace Exaroton
{
    public class Account
    {
        [JsonProperty("name")] public string Name { get; private set; }
        [JsonProperty("email")] public string Email { get; private set; }
        [JsonProperty("verified")] public bool IsVerified { get; private set; }
        [JsonProperty("credits")] public double Credits { get; private set; }

        public static async Task<Account> GetAccount()
        {
            var response = await APIClient.GetRequestAsync("https://api.exaroton.com/v1/account/");

            return response.BuildData<Account>();
        }

        public async Task<List<Server>> GetServers()
        {
            var response = await APIClient.GetRequestAsync("https://api.exaroton.com/v1/servers/");

            return response.BuildData<List<Server>>();
        }

        [JsonConstructor]
        private Account(string name, string email, bool isVerified, double credits)
        {
            Name = name;
            Email = email;
            IsVerified = isVerified;
            Credits = credits;
        }
    }
}