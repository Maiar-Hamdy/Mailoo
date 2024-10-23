using Mailo.Models;
using Microsoft.AspNetCore.Mvc;

namespace Mailo.Controllers
{
    public class ContactController : Controller
    {
   

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        
        [HttpPost]
        public IActionResult Index(Contact model)
        {
            if (ModelState.IsValid)
            {
               
                TempData["Success"] = "Your message has been sent successfully!";
                return RedirectToAction("Index");
            }

            return View(model);
        }
    }
}
