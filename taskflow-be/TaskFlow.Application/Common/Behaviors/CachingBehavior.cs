using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Application.Common.Behaviors
{
    public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : ICacheableQuery<TResponse>
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

        public CachingBehavior(IDistributedCache cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            // Try to get from cache
            var cachedValue = await _cache.GetStringAsync(request.CacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(cachedValue))
            {
                _logger.LogInformation("Cache hit for key {CacheKey}", request.CacheKey);
                var cachedResponse = JsonSerializer.Deserialize<TResponse>(cachedValue);
                if (cachedResponse != null)
                {
                    return cachedResponse;
                }
            }

            _logger.LogInformation("Cache miss for key {CacheKey}", request.CacheKey);
            var response = await next();

            // Cache the response
            if (request.SlidingExpirationSeconds > 0)
            {
                var options = new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromSeconds(request.SlidingExpirationSeconds)
                };
                var serializedResponse = JsonSerializer.Serialize(response);
                await _cache.SetStringAsync(request.CacheKey, serializedResponse, options, cancellationToken);
                _logger.LogInformation("Cached response for key {CacheKey} with sliding expiration {Seconds}s", request.CacheKey, request.SlidingExpirationSeconds);
            }

            return response;
        }
    }
}
