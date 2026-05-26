using System.Fabric;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ExpensesApi.Data;
using ExpensesApi.Infrastructure;
using ExpensesApi.Options;
using ExpensesApi.Services;
using CrossService;
using CrossService.Access;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace ExpensesApi;

internal sealed class ExpensesApiService : StatelessService
{
    public ExpensesApiService(StatelessServiceContext context)
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
                    ServiceEventSource.Current.ServiceMessage(serviceContext, $"ExpensesApi Kestrel: {url}");

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
                            "ConnectionStrings:DefaultConnection je prazan. Proveri ApplicationParameters (ExpensesApi_DefaultConnection) ili appsettings.json.");

                    builder.Services.AddDbContext<ExpensesDbContext>(options =>
                        options.UseSqlServer(connectionString));

                    builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
                    var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                        ?? throw new InvalidOperationException("Sekcija Jwt u konfiguraciji nedostaje.");
                    if (string.IsNullOrWhiteSpace(jwt.SigningKey) || jwt.SigningKey.Length < 32)
                        throw new InvalidOperationException("Jwt:SigningKey mora imati najmanje 32 karaktera (isti ključ kao kod Web1 auth servisa).");

                    builder.Services.AddCrossServiceClients(builder.Configuration);
                    builder.Services.AddScoped<ITravelPlanAccessGuard, RemoteTravelPlanAccessGuard>();
                    builder.Services.AddScoped<IExpenseService, ExpenseService>();
                    builder.Services.AddScoped<IAdminPlanNotificationService, AdminPlanNotificationService>();

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
                    builder.Services.AddSwaggerGen(c =>
                    {
                        c.SwaggerDoc("v1", new OpenApiInfo
                        {
                            Title = "Troškovi i budžet (mikroservis)",
                            Version = "v1",
                            Description = "CRUD troškova i pregled budžeta po planu; JWT izdaje Web1 /auth."
                        });
                        c.AddJwtBearerSecurity();
                    });
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
                        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Expenses API v1"));
                    }

                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.MapControllers();

                    app.MapGet("/api/v1/health", () => Results.Ok(new { status = "ok", service = "ExpensesApi" }));
                    app.MapGet("/api/v1/health/db", async (ExpensesDbContext db, CancellationToken ct) =>
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
        ];
    }
}
