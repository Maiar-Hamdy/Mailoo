using Mailo.IRepo;
using Mailo.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Mailo.Controllers
{
    public class HomeController : Controller
    {
       // private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly List<Product> _products;
        public HomeController( IUnitOfWork unitOfWork)
        {
                // _logger = logger;
                 _unitOfWork = unitOfWork; 
     

        }
        public ActionResult Index()
        {
 


            return View();
        }



    }
}
