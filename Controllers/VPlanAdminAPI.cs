using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using VPlan_API_Adapter.Attributes;

namespace VPlan_API_Adapter.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VPlanAdminAPI : ControllerBase
    {
        private Config cfg;
        private CacheKeeper cacheKeeper;
        private TokenManager tokenManager;

        public VPlanAdminAPI(Config cfg, CacheKeeper cacheKeeper, TokenManager tokenManager)
        {
            this.cfg = cfg;
            this.cacheKeeper = cacheKeeper;
            this.tokenManager = tokenManager;
        }

        [HttpGet("cache-stats")]
        [AdminApiToken]
        [ProducesJSONandXML]
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

        [HttpGet("token")]
        [AdminApiToken]
        [SecretRequired]
        [Produces("text/plain")]
        public IActionResult NewToken([FromQuery] bool admin = false)
        {
            return Request.ProduceResult(tokenManager.NewToken(admin).token);
        }

        [HttpDelete("token")]
        [AdminApiToken]
        [SecretRequired]
        public IActionResult DeleteToken([FromQuery] string token)
        {
            return tokenManager.DeleteToken(token) ? Ok() : NotFound();
        }
    }
}
