using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenIdConnect.SampleApp.Models;
using OpenIdConnect.SampleApp.Models.Home;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace OpenIdConnect.SampleApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger
            , IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            var model = new HomeModel()
            {
                AuthorizeUri = _configuration.GetSection("oidc:authorize_uri")?.Value,
                TokenUri = _configuration.GetSection("oidc:token_uri")?.Value,
                ClientId = _configuration.GetSection("oidc:client_id")?.Value,
                ClientSecret = _configuration.GetSection("oidc:client_secret")?.Value,
                RedirectUri = _configuration.GetSection("oidc:redirect_uri")?.Value,
                Scope = _configuration.GetSection("oidc:scope")?.Value,
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
