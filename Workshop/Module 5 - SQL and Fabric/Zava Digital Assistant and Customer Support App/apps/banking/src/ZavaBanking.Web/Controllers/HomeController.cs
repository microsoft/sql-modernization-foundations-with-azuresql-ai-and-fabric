using Microsoft.AspNetCore.Mvc;

namespace ZavaBanking.Web.Controllers;

public sealed class HomeController : Controller
{
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Index() => View();

    public IActionResult Error() => Problem("The website could not complete this request. Please try again.");
}