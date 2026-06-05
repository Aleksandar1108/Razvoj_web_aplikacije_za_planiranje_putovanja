using System.Fabric;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ApiGateway.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace ApiGateway;

internal sealed class ApiGatewayService : StatelessService
{
    public ApiGatewayService(StatelessServiceContext context)
        : base(context)
    {
    }

    protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
    {
        return
        [
            new ServiceInstanceListener(serviceContext =>
                new KestrelCommunicationListener(serviceContext, "ServiceEndpoint", (url, listener) =>
                {
                    ServiceEventSource.Current.ServiceMessage(serviceContext, $"ApiGateway Kestrel: {url}");

                    var builder = WebApplication.CreateBuilder();

                    builder.Services.AddSingleton(serviceContext);
                    builder.Services.AddSingleton<Infrastructure.RemotingServices>();
                    builder.WebHost
                        .UseKestrel()
                        .UseContentRoot(Directory.GetCurrentDirectory())
                        .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.None)
                        .UseUrls(url);

                    builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
                    var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                        ?? throw new InvalidOperationException("Sekcija Jwt u konfiguraciji nedostaje.");
                    if (string.IsNullOrWhiteSpace(jwt.SigningKey) || jwt.SigningKey.Length < 32)
                        throw new InvalidOperationException("Jwt:SigningKey mora imati najmanje 32 karaktera.");

                    builder.Services.AddCors(options =>
                    {
                        options.AddDefaultPolicy(policy =>
                        {
                            policy.AllowAnyHeader()
                                .AllowAnyMethod()
                                .SetIsOriginAllowed(_ => true);
                        });
                    });

                    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                        .AddJwtBearer(options =>
                        {
                            options.TokenValidationParameters = new TokenValidationParameters
                            {
                                ValidateIssuer = true,
                                ValidateAudience = true,
                                ValidateLifetime = true,
                                ValidateIssuerSigningKey = true,
                                ValidIssuer = jwt.Issuer,
                                ValidAudience = jwt.Audience,
                                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                                ClockSkew = TimeSpan.FromMinutes(1),
                                NameClaimType = JwtRegisteredClaimNames.Sub,
                                RoleClaimType = ClaimTypes.Role
                            };
                        });

                    builder.Services.AddAuthorization();
                    builder.Services.AddControllers();

                    var app = builder.Build();

                    app.UseCors();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.MapControllers();
                    app.MapGet("/api/v1/health", () => Results.Ok(new { status = "ok", service = "ApiGateway" }));

                    return app;
                }))
        ];
    }
}
