
namespace VPlan_API_Adapter
{
    public class CachePurgeService : BackgroundService
    {
        Config cfg;
        CacheKeeper cacheKeeper;

        public CachePurgeService(Config config, CacheKeeper cacheKeeper)
        {
            this.cfg = config;
            this.cacheKeeper = cacheKeeper;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using PeriodicTimer timer = new(cfg.CachePurgeInterval);
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                 cacheKeeper.Purge();
            }
        }
    }
}
