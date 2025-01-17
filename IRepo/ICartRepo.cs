using Mailo.Data.Enums;
using Mailo.Models;
using Microsoft.AspNetCore.Mvc;

namespace Mailo.IRepo
{
    public interface ICartRepo
    {

        Task<List<Product>> GetProducts(Order order);
        Task<OrderProduct> IsMatched(Order order, int id, Sizes size);
        Task<Order> GetOrder(User user);
        Task<OrderProduct> ExistingCartItem(int productId,Sizes size, User user);

        void InsertToCart(int OrderId, int ProductId, Sizes size);
        void DeleteFromCart(int OrderId, int ProductId, Sizes size);
        Task<Order> GetOrCreateCart(User user);
        Task<List<Order>> GetOrders(User user);


    }
}