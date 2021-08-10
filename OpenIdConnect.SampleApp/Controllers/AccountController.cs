using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenIdConnect.SampleApp.Models.Account;

namespace OpenIdConnect.SampleApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IMemoryCache _cache;
        private readonly IDataProtectionProvider _provider;
        private readonly IDataSerializer<OidcState> _serializer;

        public AccountController(ILogger<AccountController> logger
            , IConfiguration configuration
            , IHttpClientFactory clientFactory
            , IMemoryCache cache
            , IDataProtectionProvider provider
            , IDataSerializer<OidcState> serializer)
        {
            _logger = logger;
            _configuration = configuration;
            _clientFactory = clientFactory;
            _cache = cache;
            _provider = provider;
            _serializer = serializer;
        }

        [HttpGet("/sign-in")]
        public IActionResult SignIn()
        {
            string clientId = _configuration.GetSection("oidc:client_id").Value;
            string scope = _configuration.GetSection("oidc:scope").Value;
            string redirectUri = _configuration.GetSection("oidc:redirect_uri").Value;

            _logger.LogDebug($"client_id: {clientId}");
            _logger.LogDebug($"scope: {scope}");
            _logger.LogDebug($"redirect_uri: {redirectUri}");

            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            var codeVerifier = Microsoft.AspNetCore.Authentication.Base64UrlTextEncoder.Encode(bytes);

            _logger.LogDebug($"codeVerifier: {codeVerifier}");
            
            string codeChallenge = this.CreateCodeChallenge(codeVerifier);

            _logger.LogDebug($"code_challenge: {codeChallenge}");

            var stateData = new OidcState()
            {
                CodeVerifierCacheId = Guid.NewGuid().ToString(),
                CodeChallenge = codeChallenge
            };

            var proctor = this._provider.CreateProtector("oidc");
            var protectedData = proctor.Protect(_serializer.Serialize(stateData));

            string state = Microsoft.AspNetCore.WebUtilities.Base64UrlTextEncoder.Encode(protectedData);

            _logger.LogDebug($"state: {state}");

            _cache.Set(stateData.CodeVerifierCacheId, new OidcCacheItem() { CodeVerifier = codeVerifier, State = state }, new MemoryCacheEntryOptions()
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(1),
            });

            string authorizeUri = _configuration.GetSection("oidc:authorize_uri").Value;

            string uri = $"{authorizeUri}?client_id={clientId}&response_type=code&scope={scope}&redirect_uri={Uri.EscapeUriString(redirectUri)}&code_challenge={codeChallenge}&code_challenge_method=S256&state={state}";

            _logger.LogDebug($"authorize request: {uri}");

            return Redirect(uri);
        }

        [HttpGet("/callback")]
        public async Task<IActionResult> SignInCallback(
            [FromQuery] string code,
            [FromQuery] string state)
        {
            if (string.IsNullOrEmpty(code) == true) return BadRequest();
            if (string.IsNullOrEmpty(state) == true) return BadRequest();

            _logger.LogDebug("Callback received.");
            _logger.LogDebug($"code: {code}");
            _logger.LogDebug($"state: {state}");

            var protector = _provider.CreateProtector("oidc");
            var stateData = _serializer.Deserialize(protector.Unprotect(Microsoft.AspNetCore.WebUtilities.Base64UrlTextEncoder.Decode(state)));

            if(stateData == null)
            {
                // error
            }

            var cacheItem = _cache.Get(stateData.CodeVerifierCacheId) as OidcCacheItem;

            if(cacheItem == null)
            {
                // error
            }

            if (state.Equals(cacheItem.State) != true)
            {
                // 인증 요청 시 보낸 state 값과 같지 않음
                // error
            }

            string codeVerifier = cacheItem.CodeVerifier;
            _cache.Remove(stateData.CodeVerifierCacheId);

            string clientId = _configuration.GetSection("oidc:client_id").Value;
            string redirectUri = _configuration.GetSection("oidc:redirect_uri").Value;
            string clientSecret = _configuration.GetSection("oidc:client_secret").Value;
            string tokenUri = _configuration.GetSection("oidc:token_uri").Value;

            var client = _clientFactory.CreateClient();

            var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "code", code },
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "redirect_uri", redirectUri },
                { "grant_type", "authorization_code" },
                { "code_verifier", codeVerifier }
            });

            _logger.LogDebug($"Token issue request: {tokenUri}");

            var result = await client.PostAsync(tokenUri, form);

            string content = await result.Content.ReadAsStringAsync();
            _logger.LogDebug("Token received.");
            _logger.LogDebug("=====================================");
            _logger.LogDebug(content);
            _logger.LogDebug("=====================================");

            var oidcToken = JsonConvert.DeserializeObject<OidcToken>(content);

            if(oidcToken.IdToken == null)
            {
                // error
            }

            var idToken = new JwtSecurityToken(oidcToken.IdToken);
            string subject = idToken.Subject;

            _logger.LogDebug($"subject: {subject}");

            var user = await GetUserInfo(oidcToken.AccessToken);

            _logger.LogDebug("User:");
            _logger.LogDebug("=====================================");
            _logger.LogDebug(JsonConvert.SerializeObject(user));
            _logger.LogDebug("=====================================");

            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("id", user.Id),
                new Claim("sabun", user.Sabun),
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties()
            {
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(3),
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), authProperties);

            _logger.LogDebug("SiginIn successl");

            return RedirectToAction("Index", "Home");
        }

        private async Task<UserInfo> GetUserInfo(string accessToken)
        {
            string userInfoUri = _configuration.GetSection("oidc:userinfo_uri").Value;

            var client = _clientFactory.CreateClient();
            
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            string userInfoJson = await client.GetStringAsync(userInfoUri);

            var userInfo = JsonConvert.DeserializeObject<UserInfo>(userInfoJson);

            return userInfo;
        }

        private string CreateCodeChallenge(string codeVerifier)
        {
            var challengeBytes = SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier));
            var codeChallenge = WebEncoders.Base64UrlEncode(challengeBytes);

            return codeChallenge;
        }

        public async Task<IActionResult> LogOut()
        {
            await this.HttpContext.SignOutAsync();

            return RedirectToAction("Index", "Home");
        }
    }
}
