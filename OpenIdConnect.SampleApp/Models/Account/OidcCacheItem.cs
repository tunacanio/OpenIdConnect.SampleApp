using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenIdConnect.SampleApp.Models.Account
{
    public class OidcCacheItem
    {
        public string CodeVerifier { get; set; }

        public string State { get; set; }
    }
}
