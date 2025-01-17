using Mailo.Data;
using Mailo.Data.Enums;
using Mailo.IRepo;
using Mailo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mailo.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _db;
        public EmployeeController(IUnitOfWork unitOfWork, AppDbContext db)
        {
            _db = db;
            _unitOfWork = unitOfWork;
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            return View(await _unitOfWork.employees.GetAll());
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> New()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult New(Employee employee)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.employees.Insert(employee);
                TempData["Success"] = "Employee Has Been Added Successfully";
                return RedirectToAction("Index");
            }
            return View(employee);
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            if (id != 0)
            {
                return View(await _unitOfWork.employees.GetByID(id));
            }
            return NotFound();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize("Admin")]
        public IActionResult Edit(Employee employee)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.employees.Update(employee);
                TempData["Success"] = "Employee Has Been Updated Successfully";
                return RedirectToAction("Index");
            }
            return View(employee);
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id = 0)
        {
            if (id != 0)
            {
                return View(await _unitOfWork.employees.GetByID(id));
            }
            return NotFound();
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Employee employee)
        {

            if (employee != null)
            {
                _unitOfWork.employees.Delete(employee);
                TempData["Success"] = "Employee Has Been Deleted Successfully";
                return RedirectToAction("Index");
            }
            return NotFound();

        }
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> ViewOrders()
        {
            var orders = await _unitOfWork.orders.GetAllWithIncludes(
                order=>order.employee,
                order=>order.Payment,
                order=>order.user
             );

            if (orders == null || orders.Any(o => o == null))
            {
                TempData["ErrorMessage"] = "Orders list is null";
                return View();
            }

            // Fetch users separately and assign to each order
            foreach (var order in orders)
            {
                if (order.UserID != 0)
                {
                    order.user = await _unitOfWork.users.GetByIDWithIncludes(order.UserID); // Fetch and assign the user manually
                }
            }
            var available = orders
                .Where(o => o != null && o.OrderStatus == OrderStatus.Pending && o.EmpID == null)
                .ToList();
            if (available == null || !available.Any()) // Check if available orders are found
            {
                TempData["ErrorMessage"] = "No available orders";
                return View();
            }
            return View(available);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> AcceptOrder(Order order)
        {
            Employee employee = await _db.Employees.Include(e => e.orders).FirstOrDefaultAsync(x => x.Email == User.Identity.Name);
            order.EmpID = employee.ID;
            _unitOfWork.orders.Update(order);

            TempData["Success"] = "Order Has Been Accepted Successfully";
            return RedirectToAction("ViewRequiredOrders");
        }
        public async Task<IActionResult> ViewRequiredOrders()
        {
            Employee employee = await _db.Employees.Include(e => e.orders).FirstOrDefaultAsync(x => x.Email == User.Identity.Name);
            var orders = await _unitOfWork.orders.GetAllWithIncludes(
                order => order.employee,
                order => order.Payment,
                order => order.user
             );
            if (orders == null || orders.Any(o => o == null))
            {
                TempData["ErrorMessage"] = "Orders list is null";
                return BadRequest(TempData["ErrorMessage"]);
            }
            foreach (var order in orders)
            {
                if (order.UserID != 0)
                {
                    order.user = await _unitOfWork.users.GetByID(order.UserID);
                }
            }
            var available = orders.Where(o => o.EmpID == employee.ID).ToList();
            if (available == null || !available.Any())
            {
                TempData["ErrorMessage"] = "No available orders";
                return View();
            }
            return View(available);

        }
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> EditOrder(int OrderId)
        {
            var order =await _unitOfWork.orders.GetByIDWithIncludes(OrderId,
                order => order.employee,
                order => order.Payment,
                order => order.user,
                order=>order.OrderProducts
             );
            return View(order);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> EditOrder(int OrderId ,OrderStatus os,PaymentStatus ps)
        {
            var order = await _unitOfWork.orders.GetByIDWithIncludes(OrderId,
               order => order.employee,
               order => order.Payment,
               order => order.user,
               order => order.OrderProducts
            );
            if (ModelState.IsValid)
            {
                order.OrderStatus = os;
                order.Payment.PaymentStatus=ps;
                _unitOfWork.orders.Update(order);
                TempData["Success"] = "Order Has Been Updated Successfully";
                return RedirectToAction("ViewRequiredOrders");
                
            }
            return View(order);
        }
    }
}