using Microsoft.Net.Http.Headers;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace VPlan_API_Adapter
{
    public class TokenManager
    {
        public const string tokenHeaderName = "X-API-Token";
        public const string secretHeaderName = "X-Secret";

        const string tokenLogFile = "tokenlog.log";
        const string tokenFile = "tokens.xml";

        private List<TokenRecord> tokens = [];

        private Config cfg;
        private ILogger<TokenManager> logger;
        private IWebHostEnvironment env;

        private int requestCounter = 0;

        public TokenManager(Config cfg, ILogger<TokenManager> logger, IWebHostEnvironment env)
        {
            this.cfg = cfg;
            this.logger = logger;
            this.env = env;
            if (File.Exists(tokenFile))
            {
                XDocument doc = XDocument.Load(tokenFile);
                tokens = doc.Root!.Elements().Select(e => new TokenRecord(e.Value, DateTime.Parse(e.Attribute("lastUsed")?.Value ?? DateTime.Now.ToString("O")), bool.Parse(e.Attribute("admin")?.Value ?? "false"))).ToList();
            } else
            {
                var tr = NewToken(true);
                logger.LogInformation("Tokens reset; new admin token generated: {Token}", tr.token);
            }

            if (env.IsDevelopment()) logger.LogInformation("[DEBUG] Current secret: {Secret}", ProduceSecret());
        }

        public enum VerificationResult
        {
            Passed,
            Missing,
            Failed,
            Unauthorized
        }

        public VerificationResult VerifyTokenInRequest(HttpContext ctx, bool requireAdmin = false)
        {
            if (!ctx.Request.Headers.TryGetValue(tokenHeaderName, out var strings))
            {
                return VerificationResult.Missing;
            }

            if (tokens.Any(t => t.token == strings.First())) {
                var tr = tokens.First(r => r.token == strings.First());
                if ((requireAdmin && tr.isAdmin) || !requireAdmin)
                {
                    tr.Use();
                    requestCounter++;
                    if (requestCounter >= cfg.RequestsForSave)
                    {
                        requestCounter = 0;
                        SaveTokens();
                    }
                    LogTokenAccess(strings.First()!, ctx.Request.Path, tr.isAdmin, ctx.Connection.RemoteIpAddress!.ToString(), true, requireAdmin);
                    return VerificationResult.Passed;
                } else
                {
                    LogTokenAccess(strings.First()!, ctx.Request.Path, tr.isAdmin, ctx.Connection.RemoteIpAddress!.ToString(), false, true);
                    return VerificationResult.Unauthorized;
                }
            } else
            {
                LogTokenAccess(strings.First()!, ctx.Request.Path, false, ctx.Connection.RemoteIpAddress!.ToString(), false);
                return VerificationResult.Failed;
            }
        }

        public void LogTokenAccess(string token, string endpoint, bool adminToken, string ip, bool success, bool adminRequired = false)
        {
            string header = success ? "SUCCESS" : "FAILURE";
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] TOKEN | {header} | {token} | {endpoint} | {ip}";
            if (adminRequired) line += " | admin required";
            if (adminToken) line += " | admin provided";
            File.AppendAllText(tokenLogFile, line + '\n');
        }

        public string ProduceSecret()
        {   
            return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(cfg.Secret + "|" + DateTime.Now.ToString("yyyy-MM-dd"))));
        }

        public VerificationResult CheckSecret(HttpContext ctx)
        {
            if (ctx.Request.Headers.TryGetValue(secretHeaderName, out var strings))
            {
                bool match = strings.First() == ProduceSecret();
                string header = match ? "SUCCESS" : "FAILURE";
                string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] SECRET | {header} | {ctx.Request.Path} | {ctx.Connection.RemoteIpAddress}";
                File.AppendAllText(tokenLogFile, line + '\n');

                return match ? VerificationResult.Passed : VerificationResult.Failed;
            } else
            {
                return VerificationResult.Missing;
            }
        }

        public void SaveTokens()
        {
            XDocument doc = new();
            XElement root = new("Tokens", tokens.Select(tr => tr.ToXML()));
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

        public bool DeleteToken(string token)
        {
            if (tokens.Count <= 1) return false;
            int res = tokens.RemoveAll(r => r.token == token);
            SaveTokens();
            return res > 0;
        }
    }

    public class TokenRecord(string token, DateTime lastUsed, bool isAdmin) : IXMLSerializable
    {
        public readonly string token = token;
        public DateTime lastUsed = lastUsed;
        public readonly bool isAdmin = isAdmin;

        public void Use()
        {
            lastUsed = DateTime.Now;
        }

        public XElement ToXML()
        {
            XElement e = new("Token");
            e.SetAttributeValue("lastUsed", lastUsed.ToString("O"));
            e.SetAttributeValue("admin", isAdmin);
            e.Value = token;
            return e;
        }
    }
}
