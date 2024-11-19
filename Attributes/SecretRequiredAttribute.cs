using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace VPlan_API_Adapter.Attributes
{
    public class SecretRequiredAttribute : Attribute, IActionFilter, IHeaderFilter
    {
        public string HeaderName => TokenManager.secretHeaderName;

        public string? HeaderDescription => "The daily secret used to allow this operation";

        public bool IsHeaderRequired => true;

        public void OnActionExecuted(ActionExecutedContext context)
        {

        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var tm = context.HttpContext.RequestServices.GetRequiredService<TokenManager>();
            switch (tm.CheckSecret(context.HttpContext))
            {
                case TokenManager.VerificationResult.Missing:
                    {
                        context.Result = new ContentResult()
                        {
                            StatusCode = 400,
                            Content = "[MISSING_SECRET_HEADER]\nMissing " + TokenManager.secretHeaderName + " header"
                        };
                        return;
                    }
                case TokenManager.VerificationResult.Failed:
                    {
                        context.Result = new ContentResult()
                        {
                            StatusCode = 401,
                            Content = "[INVALID_SECRET]\ninvalid secret provided in " + TokenManager.secretHeaderName + " header"
                        };
                        return;
                    }
            }
        }
    }
}
