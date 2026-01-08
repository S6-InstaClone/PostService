using Microsoft.Extensions.Caching.Hybrid;

namespace PostService.Caching;

/// <summary>
/// Cache entry options matching research document TTLs
/// </summary>
public static class CacheOptions
{
    // Posts: 15 minute TTL
    public static readonly HybridCacheEntryOptions PostCache = new()
    {
        LocalCacheExpiration = TimeSpan.FromMinutes(2),
        Expiration = TimeSpan.FromMinutes(15)
    };

    // Feed: 60 second TTL (high churn)
    public static readonly HybridCacheEntryOptions FeedCache = new()
    {
        LocalCacheExpiration = TimeSpan.FromSeconds(15),
        Expiration = TimeSpan.FromSeconds(60)
    };
}

/// <summary>
/// Consistent cache key generation
/// </summary>
public static class CacheKeys
{
    public static string Post(int postId) => $"post:{postId}";
    public static string PostsByUser(string odId) => $"posts:user:{odId}";
    public static string Feed(int page) => $"feed:page:{page}";
}