using Mailo.Data;
using Mailo.Data.Enums;
using Mailo.IRepo;
using Mailo.Models;
using Mailo.Repo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Mailo.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartRepo _order;
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _db;
        public CartController(ICartRepo order, IUnitOfWork unitOfWork, AppDbContext db)
        {
            _order = order;
            _unitOfWork = unitOfWork;
            _db = db;
        }
        public async Task<IActionResult> Index()
        {
            User? user = await _unitOfWork.userRepo.GetUser(User.Identity.Name);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            Order? cart = await _order.GetOrCreateCart(user);
            if (cart == null || cart.OrderProducts == null)
            {
                return View();

            }
            return View(cart.OrderProducts.Select(op => op.product).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearCart()
        {
            User? user = await _unitOfWork.userRepo.GetUser(User.Identity.Name);
            var cart = await _order.GetOrCreateCart(user);
            if (cart != null)
            {
                cart.OrderProducts.Clear();
                _unitOfWork.orders.Delete(cart);
            }
            else
            {
                return View("Index");

            }

            return RedirectToAction("Index");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(Product product,int quantity)
        {
            User? user = await _unitOfWork.userRepo.GetUser(User.Identity.Name);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }
            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found";
                return BadRequest(TempData["ErrorMessage"]);

            }

            var cart = await _order.GetOrCreateCart(user);

            if (cart == null)
            {
                cart = new Order
                {
                    UserID = user.ID,
                    OrderPrice = product.TotalPrice,
                    OrderAddress = user.Address,
                    OrderProducts = new List<OrderProduct>()
                };

                _unitOfWork.orders.Insert(cart);
                await _unitOfWork.CommitChangesAsync();
                cart.OrderProducts.Add(new OrderProduct
                {
                    ProductID = product.ID,
                    Sizes=product.Sizes,
                    Quantity=quantity,
                    OrderID = cart.ID
                });
                Payment payment=new Payment
                {
                    PaymentMethod = PaymentMethod.Cash_On_Delivery,
                    UserID = user.ID,
                    OrderId = cart.ID,
                    PaymentStatus = PaymentStatus.Pending,
                    TotalPrice = cart.TotalPrice
                };
                _unitOfWork.payments.Insert(payment);
                cart.Payment=payment;
                _unitOfWork.orders.Update(cart);
                await _unitOfWork.CommitChangesAsync();
            }
            else
            {
                bool? existingOrderProduct = cart.OrderProducts
                    .Where(op => op.ProductID == product.ID && op.Sizes==product.Sizes).Any();

                if (existingOrderProduct==true)
                {
                    TempData["ErrorMessage"] = "Product is already in cart";
                    return BadRequest(TempData["ErrorMessage"]);

                }
                else
                {
                    cart.OrderPrice += product.TotalPrice;

                    cart.OrderProducts.Add(new OrderProduct
                    {
                        ProductID = product.ID,
                        Sizes=product.Sizes,
                        OrderID = cart.ID,
                        Quantity= quantity
                    });

                    _unitOfWork.orders.Update(cart);
                    await _unitOfWork.CommitChangesAsync();
                }
            }

            return RedirectToAction("Index_U", "User");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveProduct(int productId,Sizes size)
        {
            User? user = await _unitOfWork.userRepo.GetUser(User.Identity.Name);
            var cart = await _order.GetOrCreateCart(user);
            if (cart == null)
            {
                return View("Index");
            }
            else
            {
                var orderProduct = cart.OrderProducts.FirstOrDefault(op => op.ProductID == productId && op.Sizes==size);
                if (orderProduct != null)
                {
                    var product = await _unitOfWork.productRepo.GetByID(productId,size);
                    cart.OrderPrice -= product.TotalPrice;
                    cart.OrderProducts.Remove(orderProduct);
                    if (cart.OrderProducts == null || !cart.OrderProducts.Any())
                    {
                        await ClearCart();
                    }
                    await _unitOfWork.CommitChangesAsync();
                }
                else
                {
                    TempData["ErrorMessage"] = "Product not found";
                    return BadRequest(TempData["ErrorMessage"]);


                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NewOrder()
        {
            User? user = await _unitOfWork.userRepo.GetUser(User.Identity.Name);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var existingOrderItem = await _order.GetOrCreateCart(user);

            if (existingOrderItem == null || (existingOrderItem.OrderStatus != OrderStatus.New))
            {
                TempData["ErrorMessage"] = "Cart is already ordered";
                return BadRequest(TempData["ErrorMessage"]);
            }
            if (existingOrderItem.OrderProducts != null && existingOrderItem.OrderProducts.Any())
            {
                var products = existingOrderItem.OrderProducts.Where(op => op.OrderID == existingOrderItem.ID)
                    .Select(op => op.product)
                    .ToList();
                foreach (var product in products)
                {
                    product.Quantity -= existingOrderItem.OrderProducts.FirstOrDefault(op=>op.ProductID==product.ID && op.Sizes==product.Sizes).Quantity;
                }

                existingOrderItem.OrderStatus = OrderStatus.Pending;
                _unitOfWork.orders.Update(existingOrderItem);
                TempData["Success"] = "Cart Has Been Ordered Successfully";
                return RedirectToAction("ChoosePaymentMethod", existingOrderItem);
            }
            return View("Index");
        }
        public async Task<IActionResult> ChoosePaymentMethod(Order order)
        {
            return View(order.Payment);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChoosePaymentMethod(Payment payment)
        {
            
            if (payment.PaymentMethod == PaymentMethod.Paypal)
            {
                _unitOfWork.payments.Update(payment);
                return RedirectToAction("PaymentWithPayPal", "Payment");
            }
            return RedirectToAction("GetOrders");
        }
        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            User? user = await _unitOfWork.userRepo.GetUser(User.Identity.Name);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var orders = await _order.GetOrders(user);
            
            if (orders != null)
            {
                return View(orders);
            }
            else
            {
                return View("Index");

            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int OrderId)
        {
            User? user = await _unitOfWork.userRepo.GetUser(User.Identity.Name);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var order = await _unitOfWork.orders.GetByIDWithIncludes(OrderId,
                order => order.employee,
                order => order.Payment,
                order => order.user,
                order => order.OrderProducts
             );
            if (order != null)
            {
                var orderProducts= await _db.OrderProducts.Include(op=>op.product).Where(op=>op.OrderID==order.ID).ToListAsync();
                if (orderProducts != null && orderProducts.Any())
                {
                    List<Product> products = orderProducts.Select(op => op.product)
                        .ToList();
                    foreach (Product product in products)
                    {
                        product.Quantity += orderProducts.FirstOrDefault(op => op.ProductID == product.ID && op.Sizes == product.Sizes).Quantity;
                    }
                    _db.OrderProducts.RemoveRange(orderProducts);
                    _db.SaveChanges();
                }

                if (order.Payment != null)
                {
                    _unitOfWork.payments.Delete(order.Payment);
                }
                _unitOfWork.orders.Delete(order);
                return RedirectToAction("GetOrders");
            }
            else
            {
                return View("Index");
            }
        }
    }
}