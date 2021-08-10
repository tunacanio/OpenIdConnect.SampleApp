using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenIdConnect.SampleApp.Models.Home
{
    public class HomeModel
    {
        public string AuthorizeUri { get; set; }

        public string TokenUri { get; set; }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string RedirectUri { get; set; }

        public string Scope { get; set; }
    }
}
