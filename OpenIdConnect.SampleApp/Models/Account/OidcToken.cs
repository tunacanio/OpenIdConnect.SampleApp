using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace OpenIdConnect.SampleApp.Models.Account
{
    public class OidcToken
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }


        [JsonProperty("id_token")]
        public string IdToken { get; set; }
    }
}
