using Microsoft.Extensions.DependencyInjection;

namespace CrossService;

public static class RemotingScope
{
    public static async Task<T> ExecuteAsync<T>(
        IServiceProvider services,
        Func<IServiceProvider, CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        return await action(scope.ServiceProvider, cancellationToken);
    }

    public static async Task ExecuteAsync(
        IServiceProvider services,
        Func<IServiceProvider, CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        await action(scope.ServiceProvider, cancellationToken);
    }
}
