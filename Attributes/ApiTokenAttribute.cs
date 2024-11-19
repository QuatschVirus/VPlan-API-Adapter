using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace VPlan_API_Adapter.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ApiTokenAttribute : Attribute, IActionFilter, IHeaderFilter
    {
        public string HeaderName => TokenManager.tokenHeaderName;
        public string? HeaderDescription => "Your API token used for authentication";
        public bool IsHeaderRequired => true;

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var tm = context.HttpContext.RequestServices.GetRequiredService<TokenManager>();
            var res = tm.VerifyTokenInRequest(context.HttpContext);
            switch (res)
            {
                case TokenManager.VerificationResult.Missing:
                    {
                        context.Result = new ContentResult()
                        {
                            StatusCode = 400,
                            Content = "[MISSING_TOKEN_HEADER]\nMissing " + TokenManager.tokenHeaderName + " header"
                        };
                        return;
                    }
                case TokenManager.VerificationResult.Failed:
                    {
                        context.Result = new ContentResult()
                        {
                            StatusCode = 401,
                            Content = "[UNKNOWN_TOKEN]\nUnknown token provided in " + TokenManager.tokenHeaderName + " header"
                        };
                        return;
                    }
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class AdminApiToken : Attribute, IActionFilter, IHeaderFilter
    {
        public string HeaderName => TokenManager.tokenHeaderName;
        public string? HeaderDescription => "Your API token used for authentication. Needs to be an admin token";
        public bool IsHeaderRequired => true;

        public void OnActionExecuted(ActionExecutedContext context)
        {

        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var tm = context.HttpContext.RequestServices.GetRequiredService<TokenManager>();
            var res = tm.VerifyTokenInRequest(context.HttpContext, true);
            switch (res)
            {
                case TokenManager.VerificationResult.Missing:
                    {
                        context.Result = new ContentResult()
                        {
                            StatusCode = 400,
                            Content = "[MISSING_TOKEN_HEADER]\nMissing " + TokenManager.tokenHeaderName + " header"
                        };
                        return;
                    }
                case TokenManager.VerificationResult.Failed:
                    {
                        context.Result = new ContentResult()
                        {
                            StatusCode = 401,
                            Content = "[UNKNOWN_TOKEN]\nUnknown token provided in " + TokenManager.tokenHeaderName + " header"
                        };
                        return;
                    }
                case TokenManager.VerificationResult.Unauthorized:
                    {
                        context.Result = new ContentResult()
                        {
                            StatusCode = 403,
                            Content = "[UNAUTHORIZED]\nUnauthorized to perform admin actions"
                        };
                        return;
                    }
            }
        }
    }
}
