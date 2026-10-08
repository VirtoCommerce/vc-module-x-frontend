using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using VirtoCommerce.Platform.Core.Caching;

namespace VirtoCommerce.XFrontend.Data.Statistics.Caching;

public static class StatisticsCache
{
    public static async Task<T> GetOrCreateAsync<T>(
        IPlatformMemoryCache platformMemoryCache,
        string cacheKey,
        int expirationMinutes,
        Func<IChangeToken> createChangeToken,
        Func<Task<T>> load)
    {
        if (expirationMinutes <= 0)
        {
            return await load();
        }

        return await platformMemoryCache.GetOrCreateExclusiveAsync(cacheKey, async options =>
        {
            // Attached before the load, so a change saved while it runs expires the entry instead of being missed.
            options.AddExpirationToken(createChangeToken());
            options.AbsoluteExpirationRelativeToNow = ShorterOf(options.AbsoluteExpirationRelativeToNow, TimeSpan.FromMinutes(expirationMinutes));
            options.SlidingExpiration = null;

            return await load();
        });
    }

    // The platform default may be shorter, e.g. when caching is disabled platform-wide.
    private static TimeSpan ShorterOf(TimeSpan? configured, TimeSpan expiration)
    {
        return configured.HasValue && configured.Value < expiration ? configured.Value : expiration;
    }
}
