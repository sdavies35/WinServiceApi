using WindowsServiceApi.Authentication;
using WindowsServiceApi.Configuration;

namespace WindowsServiceApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure API Key settings
            builder.Services.Configure<ApiKeySettings>(
                builder.Configuration.GetSection(ApiKeySettings.SectionName));

            // Add Authentication
            var apiKeySettings = builder.Configuration.GetSection(ApiKeySettings.SectionName).Get<ApiKeySettings>();
            builder.Services.AddAuthentication(ApiKeyAuthenticationSchemeOptions.DefaultScheme)
                .AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                    ApiKeyAuthenticationSchemeOptions.DefaultScheme, options =>
                    {
                        options.ApiKeys = apiKeySettings?.ValidApiKeys ?? new List<string>();
                    });

            // Add Authorization
            builder.Services.AddAuthorization();

            builder.Services.AddControllers();
            
            // Configure Swagger with API Key support
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Description = "API Key needed to access the endpoints. X-API-Key: your-api-key",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Name = "X-API-Key",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
                });

                c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "ApiKey"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // Update CORS policy to be more restrictive
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("RestrictedCors", builder =>
                {
                    builder.WithOrigins("https://localhost:7001", "https://yourdomain.com") // Add your allowed origins
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                           .AllowCredentials();
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Windows Service API V1");
                });
            }

            app.UseHttpsRedirection();

            // Authentication and Authorization middleware (order matters!)
            app.UseAuthentication();
            app.UseAuthorization();
            
            app.UseCors("RestrictedCors");

            app.MapControllers();

            app.Run();
        }
    }
}
