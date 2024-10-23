using Mailo.Data.Enums;
using Mailo.IRepo;
using Mailo.Models;
using Microsoft.AspNetCore.Mvc;

namespace Mailo.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public EmployeeController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<IActionResult> Index()
        {
            return View(await _unitOfWork.employees.GetAll());
        }
        public async Task<IActionResult> New()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
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
        public async Task<IActionResult> Edit(int id = 0)
        {
            if (id != 0)
            {
                return View(await _unitOfWork.employees.GetByID(id));
            }
            return NotFound();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
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
        public async Task<IActionResult> DeleteEmployee(int id = 0)
        {
            var employee = await _unitOfWork.employees.GetByID(id);
            if (id != 0 && employee != null)
            {
                _unitOfWork.employees.Delete(employee);
                TempData["Success"] = "Employee Has Been Deleted Successfully";
                return RedirectToAction("Index");
            }
            return NotFound();

        }
        public async Task<IActionResult> AcceptOrder(int OrderId,int EmpId)
        {
            var order = await _unitOfWork.orders.GetByID(OrderId);
            order.EmpID=EmpId;
            
            TempData["Success"] = "Order Has Been Accepted Successfully";
            return RedirectToAction("Index");

        }
        public async Task<IActionResult> ViewOrders()
        {
            var orders = await _unitOfWork.orders.GetAll();
            return View(orders.Where(o=>o.OrderStatus==OrderStatus.Pending && o.EmpID==null).ToList());  
        }
        public async Task<IActionResult> ViewRequiredOrders(int EmpId)
        {
            var orders = await _unitOfWork.orders.GetAll();
            return View(orders.Where(o => o.EmpID == EmpId).ToList());
        }
        public async Task<IActionResult> EditOrder(Order order)
        {
            _unitOfWork.orders.Update(order);
            return RedirectToAction("Index");
        }
    }
}
