using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VPlan_API_Adapter.Client;
using Microsoft.OpenApi.Validations.Rules;
using System.Diagnostics;

namespace VPlan_API_Adapter.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VPlanAPIController : ControllerBase
    {
        private Config config;
        private CacheKeeper cacheKeeper;

        public VPlanAPIController(Config config, CacheKeeper cacheKeeper)
        {
            this.config = config;
            this.cacheKeeper = cacheKeeper;
        }

        /// <summary>
        /// Get a class by name
        /// </summary>
        /// <param name="refDateStr">The referenced date in the format yyyy-MM-dd</param>
        /// <param name="name">The classes name</param>
        /// <returns>The class if found</returns>
        [HttpGet("{refDateStr}/class/{name}")]
        [ApiToken]
        [Produces("application/json", "application/xml")]
        public IActionResult Class(string refDateStr, string name)
        {
            if (cacheKeeper.PreProcessRequest(refDateStr, out var plan, out var result))
            {
                var c = plan!.GetClass(name);
                if (c == null)
                {
                    return new ContentResult()
                    {
                        StatusCode = 404,
                        Content = $"[CLASS_NOT_FOUND]\nA class with the name {name} was not found"
                    };
                } else
                {
                    return new JsonResult(new Class(c));
                }
            } else
            {
                return result;
            }
        }
    }
}