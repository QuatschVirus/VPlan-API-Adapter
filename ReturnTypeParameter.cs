using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace VPlan_API_Adapter
{
    public class ReturnTypeParameter : IOperationFilter
    {
        public const string ReturnTypeHeaderName = "Return-Type";

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            operation.Parameters ??= [];

            var attr = context.MethodInfo.GetCustomAttribute<ProducesAttribute>();
            if (attr != null)
            {
                List<string> types = [.. attr.ContentTypes];

                var acceptHeader = new OpenApiParameter()
                {
                    Name = ReturnTypeHeaderName,
                    In = ParameterLocation.Header,
                    Description = "Set the content type the API should respond with. Defaults to " + types[0],
                    Schema = new OpenApiSchema()
                    {
                        Type = "string",
                        Enum = types.Select(s => new OpenApiString(s)).Cast<IOpenApiAny>().ToList(),
                        Default = new OpenApiString(types[0])
                    }
                };

                operation.Parameters.Add(acceptHeader);
            } else
            {
                var acceptHeader = new OpenApiParameter()
                {
                    Name = ReturnTypeHeaderName,
                    In = ParameterLocation.Header,
                    Description = "Set the content type the API should respond with. Defaults to application/json",
                    Schema = new OpenApiSchema()
                    {
                        Type = "string",
                        Enum = [new OpenApiString("application/json")],
                        Default = new OpenApiString("application/json"),
                    }
                };

                operation.Parameters.Add(acceptHeader);
            }
        }
    }
}
