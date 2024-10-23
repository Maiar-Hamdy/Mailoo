using Mailo.Data;
using Mailo.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Mailo.Controllers
{
   
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        public AccountController(AppDbContext appDbContext)
        {
            
            _context    = appDbContext;
        }
        public IActionResult Index()
        {
            return View(_context.Users.ToList());
        }
        public IActionResult Registration()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Registration(RegistrationViewModel model)
        {
            if (ModelState.IsValid)
            {
                User account = new User();
                account.Email = model.Email;
                account.FName = model.FName;
                account.LName = model.LName;
                account.Password = model.Password;
                account.Username = model.Username;
                account.PhoneNumber= model.PhoneNumber; 
                account.Address = model.Address;    
                account.Gender = model.Gender;
                account.UserType = model.UserType;
                try
                {
                    _context.Users.Add(account);
                    _context.SaveChanges();
                    ModelState.Clear();
                    ViewBag.Message = $"{account.FName} {account.LName} registered successfully. Please Login.";
                }
                catch (DbUpdateException ex)
                {
                    ModelState.AddModelError("", "Please enter unique Email or Password.");
                    return View(model);
                }
                return View();
            }
            return View(model);
        
        }
        public IActionResult Login() 
        {
            return View();
        }
        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _context.Users.Where(x => (x.Username == model.UsernameOrEmail || x.Email==model.UsernameOrEmail) && x.Password == model.Password).FirstOrDefault();
                if (user != null)
                {
                    var claims = new List<Claim>
                    {
                       new Claim(ClaimTypes.Name,user.Email),
                       new Claim ("Name",user.FName),
                       new Claim(ClaimTypes.Role,"User"),
                    };

                    var claimsIdentity=new ClaimsIdentity(claims,CookieAuthenticationDefaults.AuthenticationScheme);
                    HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("","Username/Email or Paasword is not correct");
                }
            }
            return View(model);
        }
        public IActionResult LogOut()
        {
           HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index","Home");
        }

        //[Authorize]
        //public IActionResult SecurePage()
        //{

        //    ViewBag.Name = HttpContext.User.Identity.Name;
        //    return View();
        //}
    }
}
