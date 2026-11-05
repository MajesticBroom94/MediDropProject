using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MediDropProject.Controllers;

[AllowAnonymous]
public class ErrorController : Controller
{
    [Route("Error")]
    public IActionResult Index()
    {
        Response.StatusCode = 500;

        return View("Error", 500);
    }

    [AllowAnonymous]
    [Route("Error/{statusCode:int}")]
    public IActionResult StatusCodeError(int statusCode)
    {
        var originalRequest = HttpContext.Features
            .Get<IStatusCodeReExecuteFeature>();

        var originalPath = originalRequest?.OriginalPath
                           ?? Request.Path.Value
                           ?? "/";

        var originalQuery = originalRequest?.OriginalQueryString ?? "";

        // Never redirect API requests or return HTML error pages.
        // The ESP32 expects HTTP status codes, not website views.
        if (originalPath.Equals("/api", StringComparison.OrdinalIgnoreCase) ||
            originalPath.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(statusCode);
        }

        // Unauthenticated website users should be sent to login
        if (statusCode == 401 && User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction(
                "Login",
                "Account",
                new { returnUrl = originalPath + originalQuery });
        }

        // All other HTTP errors use the shared Error.cshtml.
        Response.StatusCode = statusCode;
        return View("Error", statusCode);
    }
}