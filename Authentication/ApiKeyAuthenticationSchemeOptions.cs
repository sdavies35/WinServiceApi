using Microsoft.AspNetCore.Authentication;

namespace WindowsServiceApi.Authentication
{
    public class ApiKeyAuthenticationSchemeOptions : AuthenticationSchemeOptions
    {
        public const string DefaultScheme = "ApiKey";
        public string Scheme => DefaultScheme;
        public string AuthenticationType = DefaultScheme;
        public List<string> ApiKeys { get; set; } = new();
    }
}