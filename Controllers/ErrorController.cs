using Microsoft.AspNetCore.Mvc;

namespace AceJobAgency.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/{statusCode}")]
        public IActionResult Index(int statusCode)
        {
            ViewBag.StatusCode = statusCode;
            ViewBag.Message = GetErrorMessage(statusCode);
            return View();
        }

        private string GetErrorMessage(int statusCode)
        {
            return statusCode switch
            {
                404 => "The page you are looking for could not be found.",
                403 => "You do not have permission to access this resource.",
                500 => "An internal server error occurred. Please try again later.",
                _ => "An error occurred while processing your request."
            };
        }
    }
}