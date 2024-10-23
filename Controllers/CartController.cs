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
            _db = db;
            _unitOfWork = unitOfWork;
        }

        #region Index
        public async Task<IActionResult> Index()
        {
            // Get the logged-in user
            User? user = _db.Users.Where(x => x.Email == User.Identity.Name).FirstOrDefault();
            if (user == null)
            {
                return NotFound("User not found");
            }

            // Get the user's cart
            Order? cart = await _order.GetOrCreateCart(user);
            if (cart == null || cart.OrderProducts == null)
            {
                return BadRequest("Cart is empty"); // No products in the cart
            }


            // Pass the list of products to the view
            return View(cart.OrderProducts.Select(op => op.product).ToList());
        }
        #endregion

        #region ClearCart
        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            User? user = _db.Users.Where(x => x.Email == User.Identity.Name).FirstOrDefault();
            var cart = await _order.GetOrCreateCart(user);
            if (cart != null)
            {
                cart.OrderProducts.Clear();
                _unitOfWork.orders.Delete(cart);
            }
            else
            {
                return BadRequest("cart is already empty");

            }

            return RedirectToAction("Index");
        }
        #endregion

        //public async Task<IActionResult> AddProduct(int id)
        //{
        //    return RedirectToAction("AddProduct", await _unitOfWork.products.GetByID(id));
        //}

        #region AddProduct
        [HttpPost]
        public async Task<IActionResult> AddProduct(Product product)
        {
            Console.WriteLine($"*************************{product.ID} - {product.Name}*************************");
            User? user = _db.Users.Where(x => x.Email == User.Identity.Name).FirstOrDefault();
            if (user == null)
            {
                return NotFound("User not found");
            }
            if (product == null)
            {
                return NotFound("Product not found");
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
                await _unitOfWork.CommitChangesAsync(); // Save to get cart.ID generated

                // Now cart.ID exists, so we can add the OrderProduct
                cart.OrderProducts.Add(new OrderProduct
                {
                    ProductID = product.ID,
                    OrderID = cart.ID
                });
                _unitOfWork.orders.Update(cart); // Update cart
                await _unitOfWork.CommitChangesAsync();
                product.Quantity -= 1;
                _unitOfWork.products.Update(product);
                await _unitOfWork.CommitChangesAsync(); // Save changes
            }
            else
            {
                // Check if the product is already in the cart
                OrderProduct? existingOrderProduct = cart.OrderProducts
                    .Where(op => op.ProductID == product.ID)
                    .FirstOrDefault();

                if (existingOrderProduct != null)
                {
                    return BadRequest("Product is already in cart");
                }
                else
                {
                    cart.OrderPrice += product.TotalPrice;

                    // Add the new product to the cart
                    cart.OrderProducts.Add(new OrderProduct
                    {
                        ProductID = product.ID,
                        OrderID = cart.ID
                    });

                    _unitOfWork.orders.Update(cart); // Update cart
                    product.Quantity -= 1;
                    _unitOfWork.products.Update(product); // Update product quantity

                    await _unitOfWork.CommitChangesAsync(); // Save all changes
                }
            }

            return RedirectToAction("Index");
        }
        #endregion

        #region RemoveProduct
        [HttpPost]
        public async Task<IActionResult> RemoveProduct(int productId)
        {
            User? user = await _db.Users.Where(x => x.Email == User.Identity.Name).FirstOrDefaultAsync();
            var cart = await _order.GetOrCreateCart(user);
            if (cart == null)
            {
                return BadRequest("Cart is empty");
            }
            else
            {
                var orderProduct = cart.OrderProducts.FirstOrDefault(op => op.ProductID == productId);
                if (orderProduct != null)
                {
                    var product = await _unitOfWork.products.GetByID(productId);
                    cart.OrderPrice -= product.TotalPrice;
                    cart.OrderProducts.Remove(orderProduct);
                    if (cart.OrderProducts == null || !cart.OrderProducts.Any())
                    {
                        await ClearCart();
                    }
                    _db.SaveChanges();
                }
                else
                {
                    return NotFound("Product not found");

                }
            }
            return RedirectToAction("Index");
        }
        #endregion

        #region NewOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NewOrder()
        {
            var user = _db.Users.Where(x => x.Email == User.Identity.Name).FirstOrDefault();
            if (user == null)
            {
                return Unauthorized();
            }
            var existingOrderItem = await _order.GetOrder(user);
            //.FirstOrDefaultAsync(w => w.UserId == user.Id && w.ProductId == productId);

            if (existingOrderItem == null || (existingOrderItem.OrderStatus != OrderStatus.New))
            {
                // If the product is already in the wishlist, you may want to return a message
                return BadRequest("Cart is already ordered.");
            }
            var products = await _db.OrderProducts.Where(op => op.OrderID == existingOrderItem.ID)
                .Select(op => op.product)
                .ToListAsync();
            foreach (var product in products)
            {
                product.Quantity -= 1;
            }
            existingOrderItem.OrderStatus = OrderStatus.Pending;
            TempData["Success"] = "Cart Has Been Ordered Successfully";
            return RedirectToAction("Index");
        }
        #endregion

        #region DeleteFromCart
        [HttpDelete, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFromCart(int id = 0)
        {
            var user = _db.Users.Where(x => x.Email == User.Identity.Name).FirstOrDefault();
            if (user == null)
            {
                return Unauthorized();
            }
            var existingCartItem = await _order.ExistingCartItem(id, user);

            if (existingCartItem == null)
            {
                // If the product is already in the wishlist, you may want to return a message
                return BadRequest("Cart is already ordered.");
            }
            _order.DeleteFromCart(existingCartItem.OrderID, existingCartItem.ProductID);

            TempData["Success"] = "Cart Has Been Ordered Successfully";
            return RedirectToAction("Index");

        }
        #endregion
      
    }

}