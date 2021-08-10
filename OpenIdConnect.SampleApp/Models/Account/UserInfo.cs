using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace OpenIdConnect.SampleApp.Models.Account
{
    public class UserInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("sabun")]
        public string Sabun { get; set; }

        [JsonProperty("sub")]
        public string Id { get; set; }
    }
}
