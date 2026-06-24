using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rincon.Models;
using System.Diagnostics;

namespace Rincon.Controllers
{
    [AllowAnonymous]
    public class ErrorController : Controller
    {
        [Route("Error")]
        public IActionResult Index()
        {
            Response.StatusCode = StatusCodes.Status500InternalServerError;

            return View("~/Views/Shared/Error.cshtml", new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
