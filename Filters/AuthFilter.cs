using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ZeltekWeb.Filters
{
    public class AuthFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var user = context.HttpContext.Session.GetInt32("UsuarioID");

            if (user == null)
            {
                context.Result = new RedirectToActionResult("Index", "Login", null);
            }
        }
    }
}
