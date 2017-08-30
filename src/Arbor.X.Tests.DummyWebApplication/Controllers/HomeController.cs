using System;
using System.Web.Mvc;

namespace Arbor.X.Tests.DummyWebApplication.Controllers
{
    [RoutePrefix("")]
    public class HomeController : Controller
    {
        [HttpGet]
        [Route("")]
        public ActionResult Index()
        {
            return View(new HomeViewModel(DateTime.UtcNow.ToString("O")));
        }
    }
}
