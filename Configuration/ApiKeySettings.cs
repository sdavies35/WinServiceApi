namespace WindowsServiceApi.Configuration
{
    public class ApiKeySettings
    {
        public const string SectionName = "ApiKeySettings";
        public List<string> ValidApiKeys { get; set; } = new();
    }
}