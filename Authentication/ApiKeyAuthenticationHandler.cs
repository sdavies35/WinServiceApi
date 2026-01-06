using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace WindowsServiceApi.Authentication
{
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationSchemeOptions>
    {
        private const string ApiKeyHeaderName = "X-API-Key";
        
        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<ApiKeyAuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder) : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey(ApiKeyHeaderName))
            {
                return Task.FromResult(AuthenticateResult.Fail("API Key header missing"));
            }

            var providedApiKey = Request.Headers[ApiKeyHeaderName].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(providedApiKey))
            {
                return Task.FromResult(AuthenticateResult.Fail("API Key header empty"));
            }

            var validApiKeys = Options.ApiKeys;
            
            if (!validApiKeys.Contains(providedApiKey))
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid API Key"));
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "API User"),
                new Claim(ClaimTypes.NameIdentifier, providedApiKey)
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = 401;
            Response.ContentType = "application/json";
            var response = new { error = "API Key required", message = "Please provide a valid API key in the X-API-Key header" };
            return Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
        }

        protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = 403;
            Response.ContentType = "application/json";
            var response = new { error = "Access forbidden", message = "Invalid API Key provided" };
            return Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
        }
    }
}