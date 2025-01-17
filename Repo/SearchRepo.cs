using Mailo.Data;
using Mailo.IRepo;
using Mailo.Models;
using Microsoft.EntityFrameworkCore;

namespace Mailo.Repo
{
    public class SearchRepo :ISearchRepo
    {
        private readonly AppDbContext _db;
        public SearchRepo(AppDbContext db)
        {
            _db = db;
        }
        public async Task<List<Product>> Search(string text)
        {
            var products = await _db.Products.Where(p => p.Name.ToLower().Contains(text.ToLower())).ToListAsync();
            if (products == null)
                return await _db.Products.ToListAsync();
            return products;
        }
    }
}
