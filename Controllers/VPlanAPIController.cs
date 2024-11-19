using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VPlan_API_Adapter.Client;
using VPlan_API_Adapter.Attributes;
using Microsoft.OpenApi.Validations.Rules;
using System.Diagnostics;
using System.Xml.Serialization;
using System.Xml.Linq;

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
                    if (Request.ShouldReturnXML())
                    {
                        return Util.CreateXMLResult(new Class(c));
                    } else
                    {
                        return new JsonResult(new Class(c));
                    }
                }
            } else
            {
                return result;
            }
        }

        /// <summary>
        /// Get a teacher by shorthand
        /// </summary>
        /// <param name="refDateStr">The referenced date in the format yyyy-MM-dd</param>
        /// <param name="shorthand">The teachers shorthand name</param>
        /// <returns>The teacher if everything worked</returns>
        [HttpGet("{refDateStr}/teacher/{shorthand}")]
        [ApiToken]
        [ProducesJSONandXML]
        public IActionResult Teacher(string refDateStr, string shorthand)
        {
            if (cacheKeeper.PreProcessRequest(refDateStr, out var plan, out var result))
            {
                var t = plan!.GetTeacher(shorthand);
                if (t == null)
                {
                    return NoContent();
                }
                return Request.ProduceResult(new Teacher(t));
            } else return result;
        }

        /// <summary>
        /// Get a room by identifier
        /// </summary>
        /// <param name="refDateStr">The referenced date in the format yyyy-MM-dd</param>
        /// <param name="identifier">The rooms identifier name</param>
        /// <returns>The room if everything worked</returns>
        [HttpGet("{refDateStr}/room/{identifier}")]
        [ApiToken]
        [ProducesJSONandXML]
        public IActionResult Room(string refDateStr, string identifier)
        {
            if (cacheKeeper.PreProcessRequest(refDateStr, out var plan, out var result))
            {
                var r = plan!.GetRoom(identifier);
                if (r == null)
                {
                    return NoContent();
                }
                return Request.ProduceResult(new Room(r));
            }
            else return result;
        }
    }
}