using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace VortexFit.Filters;

/// <summary>
/// Redirige a Login si el usuario no ha iniciado sesión.
/// Equivalente declarativo al patrón RequireLogin() manual.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireLoginAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.HttpContext.Session.GetInt32("SocioId") is null)
        {
            var returnUrl = context.HttpContext.Request.Path;
            context.Result = new RedirectToActionResult(
                "Login", "Account", new { returnUrl });
        }
    }
}

/// <summary>
/// Redirige a Login si el usuario autenticado no tiene rol Admin.
/// Equivalente declarativo al patrón RequireAdmin() manual.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireAdminAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.HttpContext.Session.GetString("SocioRol") != "Admin")
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
        }
    }
}
