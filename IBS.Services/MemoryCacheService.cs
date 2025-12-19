using Microsoft.Extensions.Caching.Memory;

namespace IBS.Services
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
        Task SetAsync<T>(
            string key,
            T value,
            TimeSpan slidingExpiration,
            TimeSpan absoluteExpiration,
            CancellationToken cancellationToken = default);
    }

    public sealed class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _cache;

        public MemoryCacheService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _cache.TryGetValue(key, out T? value) ? value : default
            );
        }

        public Task SetAsync<T>(
            string key,
            T value,
            TimeSpan slidingExpiration,
            TimeSpan absoluteExpiration,
            CancellationToken cancellationToken = default)
        {
            _cache.Set(
                key,
                value,
                new MemoryCacheEntryOptions
                {
                    SlidingExpiration = slidingExpiration,
                    AbsoluteExpirationRelativeToNow = absoluteExpiration,
                    Size = 1
                });

            return Task.CompletedTask;
        }
    }
}
