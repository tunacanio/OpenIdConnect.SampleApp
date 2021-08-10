using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenIdConnect.SampleApp.Models.Account
{
    public class OidcState
    {
        public string CodeVerifierCacheId { get; set; }

        public string CodeChallenge { get; set; }
    }
}
