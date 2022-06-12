namespace cache_distributed_redis.Services
{
    public interface IWeatherForecastService
    {
        Task<List<string>> GetAllCitiesBasic();
        Task<List<string>> GetAllCitiesConcurrent();
    }
}
