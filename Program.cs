
using System.Reflection;

namespace VPlan_API_Adapter
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new()
                {
                    Version = "v1",
                    Title = "VPlan API",
                    Description = "AN ASP.NET Web REST-ish API for reading VPlan data in a usable format"
                });

                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));

                options.OperationFilter<SwaggerHeaderFilter>();
            });

            builder.Services.AddSingleton(Config.Load());
            builder.Services.AddSingleton<CacheKeeper, CacheKeeper>();
            builder.Services.AddSingleton<CachePurgeService, CachePurgeService>();
            builder.Services.AddHostedService(p => p.GetRequiredService<CachePurgeService>());
            builder.Services.AddSingleton<TokenManager, TokenManager>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
