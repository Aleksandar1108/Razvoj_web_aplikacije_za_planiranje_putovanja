using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SharingApi.Infrastructure;
public static class SwaggerJwtExtensions
{
    public static void AddJwtBearerSecurity(this SwaggerGenOptions c)
    {
        const string securitySchemeId = "Bearer";

        c.AddSecurityDefinition(securitySchemeId, new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = JwtBearerDefaults.AuthenticationScheme,
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT koji izdaje Web1 /auth."
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = securitySchemeId
                    }
                },
                Array.Empty<string>()
            }
        });
    }
}
