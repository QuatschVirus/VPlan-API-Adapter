using System.Xml.Linq;

namespace VPlan_API_Adapter
{
    public class TokenManager
    {
        public const string tokenHeaderName = "X-API-Token";
        const string tokenLogFile = "tokenlog.log";
        const string tokenFile = "tokens.xml";

        private List<TokenRecord> tokens = [];

        private Config cfg;
        private ILogger<TokenManager> logger;

        public TokenManager(Config cfg, ILogger<TokenManager> logger)
        {
            this.cfg = cfg;
            this.logger = logger;
            if (File.Exists(tokenFile))
            {
                XDocument doc = XDocument.Load(tokenFile);
                tokens = doc.Root!.Elements().Select(e => new TokenRecord(e.Value, DateTime.Parse(e.Attribute("lastUsed")?.Value ?? DateTime.Now.ToString("O")), bool.Parse(e.Attribute("admin")?.Value ?? "false"))).ToList();
            } else
            {
                var tr = NewToken(true);
                logger.LogInformation("Tokens reset; new admin token generated: {Token}", tr.token);
            }
            
        }

        public enum VerificationResult
        {
            Passed,
            Missing,
            Failed
        }

        public VerificationResult VerifyTokenInRequest(HttpContext ctx)
        {
            if (!ctx.Request.Headers.TryGetValue(tokenHeaderName, out var strings))
            {
                return VerificationResult.Missing;
            }

            if (tokens.Any(t => t.token == strings.First())) {
                var tr = tokens.First(r => r.token == strings.First());
                tr.Use();
                LogTokenAccess(strings.First()!, ctx.Request.Path, tr.isAdmin, ctx.Connection.RemoteIpAddress!.ToString(), true);
                return VerificationResult.Passed;
            } else
            {
                LogTokenAccess(strings.First()!, ctx.Request.Path, false, ctx.Connection.RemoteIpAddress!.ToString(), false);
                return VerificationResult.Failed;
            }
        }

        public void LogTokenAccess(string token, string endpoint, bool adminToken, string ip, bool success)
        {
            string header = success ? "SUCCESS" : "FAILURE";
            string line = $"[{header}] {token} | {endpoint} | {ip}";
            if (adminToken) line += " (admin)";
            File.AppendAllText(tokenLogFile, line + '\n');
        }

        public void SaveTokens()
        {
            XDocument doc = new();
            XElement root = new("Tokens");
            foreach (var token in tokens)
            {
                XElement e = new("Token");
                e.SetAttributeValue("lastUsed", token.lastUsed.ToString("O"));
                e.SetAttributeValue("admin", token.isAdmin);
                e.Value = token.token;
                root.Add(e);
            }
            doc.Add(root); 
            doc.Save(tokenFile);
        }

        public TokenRecord NewToken(bool admin)
        {
            string token = Guid.NewGuid().ToString();
            while (tokens.Any(r => r.token == token)) { token = Guid.NewGuid().ToString(); }
            TokenRecord tk = new(token, DateTime.Now, admin);
            tokens.Add(tk);
            SaveTokens();
            return tk;
        }
    }

    public class TokenRecord(string token, DateTime lastUsed, bool isAdmin)
    {
        public readonly string token = token;
        public DateTime lastUsed = lastUsed;
        public readonly bool isAdmin = isAdmin;

        public void Use()
        {
            lastUsed = DateTime.Now;
        }
    }
}
