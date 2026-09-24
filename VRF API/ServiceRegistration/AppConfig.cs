using Microsoft.Extensions.Caching.Distributed;
using VRF_API.Authentication;
using VRF_API.Configuration;
using VRF_API.Repository;
using VRF_API.Services;

namespace VRF_API.ServiceRegistration
{
    public static class AppConfig
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Bind JwtSettings from appsettings.json
            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

            // Bind EmailSettings from appsettings.json
            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));

            // Register application services
            // services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IVendorCreationService, VendorCreationService>();
            services.AddScoped<IHomePageService, HomePageService>();
            services.AddScoped<Log>();
            services.AddScoped<IRequestContext, RequestContext>();
            services.AddScoped<DbConnection>();
            services.AddScoped<SessionManager>();

            return services;
        }
    }
}
