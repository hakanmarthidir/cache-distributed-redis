using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace cache_distributed_redis.Services
{
    public class WeatherForecastService : IWeatherForecastService
    {
        private readonly IDistributedCache _distCache;
        const string CACHE_CITY_LIST = "citylist";

        private static readonly SemaphoreSlim GetCitiesSemaphore = new SemaphoreSlim(1, 1);
        public WeatherForecastService(IDistributedCache distCache)
        {
            _distCache = distCache;
        }

        private async Task<List<string>> GetCitiesAsync()
        {
            var result = await Task.Run<List<string>>(() =>
            {
                return new List<string>() { "Istanbul", "Stuttgart", "Esslingen", "Berlin", "Frankfurt", "Mannheim", "Tübingen" };

            });
            return result;
        }

        public async Task<List<string>> GetAllCitiesBasic()
        {
            List<string>? result;

            var citiesCache = await this._distCache.GetAsync(CACHE_CITY_LIST).ConfigureAwait(false);
            if (citiesCache == null)
            {
                result = await this.GetCitiesAsync();
                if (result != null)
                {
                    var serializedList = JsonSerializer.SerializeToUtf8Bytes(result);
                    var policy = new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(1));
                    await this._distCache.SetAsync(CACHE_CITY_LIST, serializedList, policy);                    
                }                
            }
            else
            {                
                result = JsonSerializer.Deserialize<List<string>>(citiesCache);                
            }
            return result ?? new List<string>();
        }

        public async Task<List<string>> GetAllCitiesConcurrent()
        {
            return await this.GetAllCitiesSemaphore(GetCitiesSemaphore);
        }

        private async Task<List<string>> GetAllCitiesSemaphore(SemaphoreSlim semaphore)
        {
            List<string>? cities;

            var citiesCache = await this._distCache.GetAsync(CACHE_CITY_LIST).ConfigureAwait(false);
            if (citiesCache != null) 
            {
                cities =  JsonSerializer.Deserialize<List<string>>(citiesCache);
            }
            else
            {
                try
                {
                    await semaphore.WaitAsync();

                    citiesCache = await this._distCache.GetAsync(CACHE_CITY_LIST).ConfigureAwait(false);
                    if (citiesCache != null) 
                    {
                        cities = JsonSerializer.Deserialize<List<string>>(citiesCache);
                    }
                    else
                    {
                        cities = await this.GetCitiesAsync();

                        var serializedList = JsonSerializer.SerializeToUtf8Bytes(cities);
                        var policy = new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(1));
                        await this._distCache.SetAsync(CACHE_CITY_LIST, serializedList, policy);
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }           

            return cities ?? new List<string>();
        }


    }
}
