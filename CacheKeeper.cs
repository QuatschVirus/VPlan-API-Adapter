using Microsoft.AspNetCore.Mvc;
using System.Xml.Linq;
using VPlan_API_Adapter.Server;

namespace VPlan_API_Adapter
{
    public class CacheKeeper
    {
        public struct CacheStats : IXMLSerializable
        {
            public DateTime LastUpdated { get; set; }
            public DateOnly ReferenceDate { get; set; }
            public DateTime DataExpires { get; set; }

            public DateTime LastPulled { get; set; }
            public DateTime CacheExpires { get; set; }

            public XElement ToXML()
            {
                XElement root = new("Stat");

                root.Add(new XElement("LastUpdated", LastUpdated));
                root.Add(new XElement("ReferenceDate", ReferenceDate));
                root.Add(new XElement("DataExpires", DataExpires));
                root.Add(new XElement("LastPulled", LastPulled));
                root.Add(new XElement("CacheExpires", CacheExpires));

                return root;
            }
        }

        private Config cfg;

        private Dictionary<DateOnly, (VPlan, DateTime)> caches = [];

        public CacheKeeper(Config cfg)
        {
            this.cfg = cfg;
        }

        public VPlan? GetPlan(DateOnly refDate)
        {
            if (caches.TryGetValue(refDate, out var cache))
            {
                cache.Item2 = DateTime.Now;
                return cache.Item1;
            } else
            {
                VPlan v = new(refDate, cfg.DataExpiration, cfg.BaseURL, cfg.Username, cfg.Password);
                caches.Add(refDate, (v, DateTime.Now));
                return v.UpdateData() ? v : null;
            }
        }

        public bool PreProcessRequest(string refDateStr, out VPlan? vPlan, out IActionResult result)
        {
            if (DateOnly.TryParseExact(refDateStr, "yyyy-MM-dd", out var refDate))
            {
                vPlan = GetPlan(refDate);
                if (vPlan == null)
                {
                    result = new ContentResult()
                    {
                        StatusCode = 404,
                        Content = $"[PLAN_NOT_FOUND]\nA plan for {refDate:yyyy-MM-dd} was not found"
                    };
                    
                    return false;
                }
                result = new OkResult();
                return true;
            } else
            {
                vPlan = null;
                result = new ContentResult()
                {
                    StatusCode = 404,
                    Content = $"[REFDATE_FORMAT_ERROR]\n{refDateStr} could not be converted according to yyyy-MM-dd"
                };

                return false;
            }
        }

        public XMLSerializeableList<CacheStats> GetStats()
        {
            XMLSerializeableList<CacheStats> stats = new("Stats");
            foreach (var kv in caches)
            {
                stats.Add(new()
                {
                    LastUpdated = kv.Value.Item1.LastPulled,
                    DataExpires = kv.Value.Item1.Expires,
                    ReferenceDate = kv.Key,
                    LastPulled = kv.Value.Item2,
                    CacheExpires = kv.Value.Item2 + cfg.CacheExpiration
                });
            }
            return stats;
        }

        public void Purge()
        {
            foreach (var kv in caches)
            {
                if (kv.Value.Item2 + cfg.CacheExpiration < DateTime.Now)
                {
                    caches.Remove(kv.Key);
                }
            }
        }
    }
}
