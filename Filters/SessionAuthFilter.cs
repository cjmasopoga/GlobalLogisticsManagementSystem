using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Global_Logistics_Management_System.Filters
{
    public class SessionAuthFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var controller = context.RouteData.Values["controller"]?.ToString();
            var action = context.RouteData.Values["action"]?.ToString();

            // Let the login/logout actions through without a token
            if (string.Equals(controller, "Account", StringComparison.OrdinalIgnoreCase))
                return;

            var token = context.HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                context.Result = new RedirectToActionResult("Login", "Account",
                    new { returnUrl });
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
