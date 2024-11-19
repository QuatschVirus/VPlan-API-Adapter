using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace VPlan_API_Adapter
{
    public class SwaggerHeaderFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            IEnumerable<IHeaderFilter> filters = context.MethodInfo.GetCustomAttributes().Where(a => a.GetType().IsAssignableTo(typeof(IHeaderFilter))).Cast<IHeaderFilter>();
            operation.Parameters ??= [];
            foreach (var filter in filters)
            {
                operation.Parameters.Add(new()
                {
                    Name = filter.HeaderName,
                    In = ParameterLocation.Header,
                    Description = filter.HeaderDescription,
                    Required = filter.IsHeaderRequired,
                    Schema = new OpenApiSchema() { Type = "string" }
                });
            }
        }
    }

    public interface IHeaderFilter
    {
        public string HeaderName { get; }
        public string? HeaderDescription { get; }
        public bool IsHeaderRequired { get; }
    }
}
