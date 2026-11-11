using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VirtoCommerce.Platform.Caching;

namespace VirtoCommerce.XFrontend.Tests.Infrastructure;

/// <summary>
/// The real <see cref="PlatformMemoryCache"/> (so the platform's default entry options apply), recording every entry it
/// creates so a test can read the expiration settings an entry was stored with.
/// </summary>
public sealed class RecordingPlatformMemoryCache : PlatformMemoryCache
{
    public RecordingPlatformMemoryCache(CachingOptions options = null)
        : base(new MemoryCache(new MemoryCacheOptions()), Options.Create(options ?? new CachingOptions()), NullLogger<PlatformMemoryCache>.Instance)
    {
    }

    public ConcurrentQueue<ICacheEntry> Entries { get; } = new();

    public override ICacheEntry CreateEntry(object key)
    {
        var entry = base.CreateEntry(key);
        Entries.Enqueue(entry);

        return entry;
    }
}
