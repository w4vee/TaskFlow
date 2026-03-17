namespace TaskFlow.Application.Common.Interfaces;

/// <summary>
/// Marker interface for queries that should be cached
/// </summary>
public interface ICacheableQuery<TResponse>
{
    /// <summary>
    /// Unique cache key for this query
    /// </summary>
    string CacheKey { get; }

    /// <summary>
    /// Cache expiration time in seconds (0 means use default)
    /// </summary>
    int SlidingExpirationSeconds { get; }
}
