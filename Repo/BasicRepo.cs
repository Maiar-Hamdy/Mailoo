using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Mailo.Data;
using Mailo.IRepo;
using Mailo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;


namespace Mailo.Repo
{
	public class BasicRepo<T> : IBasicRepo<T> where T : class
	{
		protected readonly AppDbContext _db;
        private readonly DbSet<T> _dbSet;
        public BasicRepo(AppDbContext db)
        {
            _db = db;
            _dbSet = db.Set<T>();
        }
        public async Task<List<T>> GetAllWithIncludes(params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            return await query.ToListAsync();
        }
        public async Task<T> GetByIDWithIncludes(int id, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;

            // Apply includes
            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            // Assuming the primary key is named "Id"
            return await query.SingleOrDefaultAsync(entity => EF.Property<int>(entity, "ID") == id);
        }
        public async Task<IEnumerable<T>> GetAll()
		{
			//modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys())
			return await _db.Set<T>().ToListAsync();
		}

		public async Task<T> GetByID(int id)
		{
			return await _db.Set<T>().FindAsync(id);
		}
		
		public async void Insert(T entity)
		{
			await _db.Set<T>().AddAsync(entity);
			_db.SaveChanges();
		}
		public  void Update(T entity)
		{
			_db.Set<T>().Update(entity);
			_db.SaveChanges();
		}
		public  void Delete(T entity)
		{
			_db.Set<T>().Remove(entity);
			_db.SaveChanges();
		}
        
    }
}
