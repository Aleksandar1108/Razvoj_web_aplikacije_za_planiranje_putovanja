using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using SharingApi.Models;

namespace SharingApi.Services;

public interface IShareAccessCache
{
    Task<string?> TryGetAsync(string cacheKey, CancellationToken cancellationToken);
    Task SetAsync(string cacheKey, string kind, CancellationToken cancellationToken);
}

public sealed class ShareAccessCache : IShareAccessCache
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);
    private const string DictionaryName = "ShareAccessCache";
    private readonly IReliableStateManager _stateManager;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private IReliableDictionary<string, ShareAccessCacheEntry>? _cache;

    public ShareAccessCache(IReliableStateManager stateManager)
    {
        _stateManager = stateManager;
    }

    private async Task<IReliableDictionary<string, ShareAccessCacheEntry>> GetCacheAsync(CancellationToken cancellationToken)
    {
        if (_cache is not null)
            return _cache;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_cache is null)
                _cache = await _stateManager.GetOrAddAsync<IReliableDictionary<string, ShareAccessCacheEntry>>(DictionaryName);
            return _cache;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<string?> TryGetAsync(string cacheKey, CancellationToken cancellationToken)
    {
        var cache = await GetCacheAsync(cancellationToken);
        using var tx = _stateManager.CreateTransaction();
        var result = await cache.TryGetValueAsync(tx, cacheKey);
        if (!result.HasValue)
            return null;

        var entry = result.Value;
        if (DateTime.UtcNow - entry.CachedAtUtc > CacheTtl)
            return null;

        return entry.Kind;
    }

    public async Task SetAsync(string cacheKey, string kind, CancellationToken cancellationToken)
    {
        var cache = await GetCacheAsync(cancellationToken);
        using var tx = _stateManager.CreateTransaction();
        await cache.AddOrUpdateAsync(
            tx,
            cacheKey,
            new ShareAccessCacheEntry { Kind = kind, CachedAtUtc = DateTime.UtcNow },
            (_, _) => new ShareAccessCacheEntry { Kind = kind, CachedAtUtc = DateTime.UtcNow });
        await tx.CommitAsync();
    }
}
