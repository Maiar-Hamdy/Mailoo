using System.Collections.Generic;
using System.Linq.Expressions;
namespace Mailo.IRepo
{
	public interface IBasicRepo<T> where T : class
	{
		Task<List<T>> GetAllWithIncludes(params Expression<Func<T, object>>[] includes);
		Task<T> GetByIDWithIncludes(int id, params Expression<Func<T, object>>[] includes);

        Task<IEnumerable<T>> GetAll();
		Task<T> GetByID(int id);
		void Insert(T entity);
        void Update(T entity);
        void Delete(T entity);
	}
}
