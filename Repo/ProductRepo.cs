using Mailo.Data;
using Mailo.Data.Enums;
using Mailo.Models;
using Mailoo.IRepo;
using Microsoft.EntityFrameworkCore;

namespace Mailoo.Repo
{
    public class ProductRepo : IProductRepo
    {
        private readonly AppDbContext _db;
        public ProductRepo(AppDbContext db)
        {
            _db = db;
        }
        public async Task<IEnumerable<Product>> GetAll()
        {
            //modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys())
            return await _db.Products.DistinctBy(p=>p.ID).ToListAsync();
        }
        public async Task<Product> GetByID(int id,Sizes size)
        {
            return await _db.Products.FindAsync(id,size);
        }
    }
}
