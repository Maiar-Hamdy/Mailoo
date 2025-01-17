using Mailo.Data.Enums;
using Mailo.Data;
using Mailo.IRepo;
using Mailo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Mailo.Controllers
{
    public class UserController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _context;
        private readonly ISearchRepo _search;

        public UserController(IUnitOfWork unitOfWork, AppDbContext context, ISearchRepo search)
        {
            _unitOfWork = unitOfWork;
            _context = context;
            _search = search;

        }
        public async Task<IActionResult> Search(string text)
        {
            User? user = await _unitOfWork.userRepo.GetUser(User.Identity.Name);
            if(user==null || user.UserType==UserType.Client)
            return View("Index_U",await _search.Search(text));
            return View("Index_A",await _search.Search(text));

        }

        #region Admin 
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index_A()
        {
            return View(await _unitOfWork.productRepo.GetAll());
        }
        public async Task<IActionResult> New()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> New(Product product,int sq,int mq,int lq)
        {
            if (ModelState.IsValid)
            {
                    if (product.clientFile != null)
                    {
                        MemoryStream stream = new MemoryStream();
                        product.clientFile.CopyTo(stream);
                        product.dbImage = stream.ToArray();
                    }
                product.Quantity = sq;
                product.Sizes = Sizes.S;
                    _unitOfWork.products.Insert(product);
                
                List<Product> products = new List<Product> { 
                new Product{ID=product.ID, Name=product.Name,Description=product.Description,Category=product.Category,AdditionDate=product.AdditionDate,Discount=product.Discount,Price=product.Price,ImageUrl=product.ImageUrl,clientFile=product.clientFile,dbImage=product.dbImage,imageSrc=product.imageSrc,Sizes=Sizes.M,Quantity=mq},
                new Product{ID=product.ID,Name=product.Name,Description=product.Description,Category=product.Category,AdditionDate=product.AdditionDate,Discount=product.Discount,Price=product.Price,ImageUrl=product.ImageUrl,clientFile=product.clientFile,dbImage=product.dbImage,imageSrc=product.imageSrc,Sizes=Sizes.L,Quantity=lq}

                };
                   await _context.Products.AddRangeAsync(products);
                await _context.SaveChangesAsync();
                    TempData["Success"] = "product Has Been Added Successfully";
                return RedirectToAction("Index_A");
            }
            else
                return View(product);
        }



        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id != 0)
            {
                return View(await _context.Products.FirstOrDefaultAsync(p=>p.ID==id));
            }
            return NotFound();
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            if (id != 0)
            {
                var product = await _context.Products.Where(p => p.ID == id).ToListAsync();
                if (product != null)
                {
                    _context.RemoveRange(product);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "product Has Been Deleted Successfully";
                    return RedirectToAction("Index_A");
                }
            }
            return NotFound();

        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            if (id != 0)
            {
                // Get the product by ID
                var product = await _unitOfWork.products.GetByID(id);
                if (product == null)
                {
                    return NotFound();
                }
                return View(product);
            }
            return NotFound();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(Product product)
        {
            var existingProduct = await _context.Products.FindAsync(product.ID);
            if (existingProduct == null)
            {
                return NotFound();
            }

            // Handle the file upload, if there is a file
            if (product.clientFile != null && product.clientFile.Length > 0)
            {
                // Save the uploaded file to a specific folder, e.g., wwwroot/images
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", product.clientFile.FileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await product.clientFile.CopyToAsync(stream);
                }

                // Update the image path in the existing product
                existingProduct.imageSrc = "~/images/" + product.clientFile.FileName;
            }

            if (ModelState.IsValid)
            {
                // Update only the necessary fields in the existing product
                existingProduct.Name = product.Name;
                existingProduct.Price = product.Price;
                existingProduct.Description = product.Description;
                existingProduct.Category = product.Category;
                // Update other fields as needed

                // Now update the existing product in the database
                _unitOfWork.products.Update(existingProduct);

                TempData["Success"] = "Product Has Been Updated Successfully";
                return RedirectToAction("Index_A");
            }

            return View(product);
        }

        #endregion



        #region Client
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> Index_U()
        {
            return View(await _unitOfWork.products.GetAll());
        }

        #endregion














    }
}
