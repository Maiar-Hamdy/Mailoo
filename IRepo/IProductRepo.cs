using Mailo.Data.Enums;
using Mailo.Models;

namespace Mailoo.IRepo
{
    public interface IProductRepo
    {
        Task<IEnumerable<Product>> GetAll();
        Task<Product> GetByID(int id, Sizes size);
    }
}
