using Microsoft.AspNetCore.Mvc;

namespace WebService.Controllers
{
    public class ActorController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}