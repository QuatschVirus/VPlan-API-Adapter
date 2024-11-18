using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace VPlan_API_Adapter.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VPlanAdminAPI : ControllerBase
    {
        private Config cfg;
        private CacheKeeper cacheKeeper;

        public VPlanAdminAPI(Config cfg, CacheKeeper cacheKeeper)
        {
            this.cfg = cfg;
            this.cacheKeeper = cacheKeeper;
        }

        [HttpGet("cache-stats")]
        [AdminApiToken]
        
        public IActionResult CacheStats()
        {
            //List<CacheKeeper.CacheStats> stats = cacheKeeper.GetStats();
            //if (Request.ShouldReturnXML())
            //{
            //    return Util.CreateXMLResult(XMLSerializeableList<CacheKeeper.CacheStats>.From(stats, "Stats"));
            //}
            //else
            //{
            //    return new JsonResult(stats);
            //}

            return Request.ProduceResult(cacheKeeper.GetStats());
        }
    }
}
