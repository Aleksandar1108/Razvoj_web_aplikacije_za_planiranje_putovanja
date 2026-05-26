using System;
using System.Collections.Generic;
using System.Fabric;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Data;
using Web1.Data;
using Web1.Infrastructure;
using Web1.Options;
using Web1.Services.Admin;
using Web1.Services.Auth;
using Web1.Services.Notifications;
using CrossService;

namespace Web1
{
    internal sealed class Web1 : StatelessService
    {
        public Web1(StatelessServiceContext context)
            : base(context)
        { }

        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[]
            {
                new ServiceInstanceListener(serviceContext =>
                    new KestrelCommunicationListener(serviceContext, "ServiceEndpoint", (url, listener) =>
                    {
                        ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel on {url}");

                        var builder = WebApplication.CreateBuilder();

                        builder.Services.AddSingleton<StatelessServiceContext>(serviceContext);
                        builder.WebHost
                                    .UseKestrel()
                                    .UseContentRoot(Directory.GetCurrentDirectory())
                                    .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.None)
                                    .UseUrls(url);

                        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")?.Trim();
                        if (string.IsNullOrWhiteSpace(connectionString))
                            throw new InvalidOperationException(
                                "ConnectionStrings:DefaultConnection je prazan. Proveri ApplicationParameters/Local.1Node.xml → Web1_DefaultConnection (Service Fabric) ili appsettings.json.");

                        builder.Services.AddDbContext<AppDbContext>(options =>
                            options.UseSqlServer(connectionString));

                        builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
                        var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                            ?? throw new InvalidOperationException("Sekcija Jwt u konfiguraciji nedostaje.");
                        if (string.IsNullOrWhiteSpace(jwt.SigningKey) || jwt.SigningKey.Length < 32)
                            throw new InvalidOperationException("Jwt:SigningKey mora imati najmanje 32 karaktera.");

                        builder.Services.AddCrossServiceClients(builder.Configuration);

                        builder.Services.AddScoped<IAuthService, AuthService>();
                        builder.Services.AddScoped<IAdminService, AdminService>();
                        builder.Services.AddScoped<INotificationService, NotificationService>();

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
                        builder.Services.AddEndpointsApiExplorer();
                        builder.Services.AddSwaggerGen(c => c.AddJwtBearerSecurity());
                        builder.Services.AddProblemDetails();

                        var app = builder.Build();
                        if (app.Environment.IsDevelopment())
                            app.UseDeveloperExceptionPage();
                        else
                            app.UseExceptionHandler();

                        app.UseCors();
                        if (app.Environment.IsDevelopment())
                        {
                        app.UseSwagger();
                        app.UseSwaggerUI();
                        }
                        app.UseAuthentication();
                        app.UseAuthorization();
                        app.MapControllers();

                        app.MapGet("/api/v1/health", () => Results.Ok(new { status = "ok" }));
                        app.MapGet("/api/v1/health/db", async (AppDbContext db, CancellationToken ct) =>
                        {
                            try
                            {
                                await db.Database.OpenConnectionAsync(ct);
                                await db.Database.CloseConnectionAsync();
                                return Results.Ok(new { status = "ok", database = "connected" });
                            }
                            catch (Exception ex)
                            {
                                return Results.Json(
                                    new { status = "error", message = ex.Message },
                                    statusCode: StatusCodes.Status503ServiceUnavailable);
                            }
                        });

                        return app;

                    }))
            };
        }
    }
}
