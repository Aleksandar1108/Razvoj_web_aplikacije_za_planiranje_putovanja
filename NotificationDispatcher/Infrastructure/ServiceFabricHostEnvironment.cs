using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace NotificationDispatcher.Infrastructure;

internal sealed class ServiceFabricHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Production;
    public string ApplicationName { get; set; } = "NotificationDispatcher";
    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
