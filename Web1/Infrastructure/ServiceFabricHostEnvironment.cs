using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Web1.Infrastructure;

internal sealed class ServiceFabricHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } =
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environments.Production;
    public string ApplicationName { get; set; } = "Web1";
    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
