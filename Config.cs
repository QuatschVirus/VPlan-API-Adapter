using System.Text.Json;

namespace VPlan_API_Adapter
{
    public class Config
    {
        const string path = "config.json";
        private static readonly JsonSerializerOptions options = new()
        {
            WriteIndented = true,
        };

        public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromHours(1);
        public TimeSpan DataExpiration { get; set; } = TimeSpan.FromMinutes(10);
        public string BaseURL { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public TimeSpan TokenExpiration { get; set; } = TimeSpan.FromDays(30);
        public TimeSpan CachePurgeInterval { get; set; } = TimeSpan.FromHours(2);
        public string Secret {  get; set; } = string.Empty;
        public int RequestsForSave { get; set; } = 10;

        public static Config Load()
        {
            bool promptFill = false;
            Config cfg = new();
            if (File.Exists(path))
            {
                cfg = JsonSerializer.Deserialize<Config>(File.ReadAllText(path))!;
            } else promptFill = true;
            File.WriteAllText(path, JsonSerializer.Serialize(cfg, options));
            if (promptFill)
            {
                Console.WriteLine("New config.json generated, make sure to fill it. Exiting...");
                Environment.Exit(17);
            }
            return cfg;
        }
    }
}
