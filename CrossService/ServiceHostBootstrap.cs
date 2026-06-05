using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CrossService;

public static class ServiceHostBootstrap
{
    public static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();
    }

    public static string RequireConnectionString(IConfiguration configuration, string name = "DefaultConnection")
    {
        var connectionString = configuration.GetConnectionString(name)?.Trim();
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                $"ConnectionStrings:{name} je prazan. Proveri ApplicationParameters ili appsettings.json.");
        return connectionString;
    }

    public static IServiceProvider BuildProvider(Action<IServiceCollection, IConfiguration> configure)
    {
        var configuration = BuildConfiguration();
        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        configure(services, configuration);
        return services.BuildServiceProvider();
    }
}
