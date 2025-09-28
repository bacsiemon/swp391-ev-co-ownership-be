using EvCoOwnership.Helpers.BaseClasses;
using EvCoOwnership.Repositories.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace EvCoOwnership.Repositories.Repositories.Base
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected EvCoOwnershipDbContext _context;

        public GenericRepository(EvCoOwnershipDbContext context)
        {
            _context = context;
        }

        public List<T> GetAll()
        {
            return _context.Set<T>().ToList();
        }
        public async Task<List<T>> GetAllAsync()
        {
            return await _context.Set<T>().ToListAsync();
        }
        public async Task<List<T>> GetAllAsync(params string[] includeProperties)
        {
            IQueryable<T> query = _context.Set<T>();

            // Eagerly load the related entities specified in includeProperties
            foreach (var includeProperty in includeProperties)
            {
                query = query.Include(includeProperty);
            }

            return await query.ToListAsync();
        }

        public async Task<PaginatedList<T>> GetPaginatedAsync(int page, int size, int firstPage = 1)
        {
            if (firstPage > page)
                throw new ArgumentException($"Page ({page}) must be greater or equal than firstPage ({firstPage})");

            IQueryable<T> query = _context.Set<T>();
            var total = await query.CountAsync();
            var items = await query.Skip((page - firstPage) * size).Take(size).ToListAsync();
            var totalPages = (int)Math.Ceiling(total / (double)size);

            return new PaginatedList<T>
            {
                Page = page,
                Size = size,
                Total = total,
                Items = items,
                TotalPages = totalPages
            };
        }

        public async Task<PaginatedList<T>> GetPaginatedAsync(int page, int size, int firstPage = 1, params string[] includeProperties)
        {
            if (firstPage > page)
                throw new ArgumentException($"Page ({page}) must be greater or equal than firstPage ({firstPage})");

            IQueryable<T> query = _context.Set<T>();

            // Eagerly load the related entities specified in includeProperties
            foreach (var includeProperty in includeProperties)
            {
                query = query.Include(includeProperty);
            }

            var total = await query.CountAsync();
            var items = await query.Skip((page - firstPage) * size).Take(size).ToListAsync();
            var totalPages = (int)Math.Ceiling(total / (double)size);

            return new PaginatedList<T>
            {
                Page = page,
                Size = size,
                Total = total,
                Items = items,
                TotalPages = totalPages
            };
        }

        public void Create(T entity)
        {
            _context.Add(entity);
        }

        public void CreateRange(List<T> entities)
        {
            _context.AddRange(entities);
        }

        public void Update(T entity)
        {
            var tracker = _context.Attach(entity);
            tracker.State = EntityState.Modified;
        }

        public void UpdateRange(List<T> entities)
        {
            foreach (var entity in entities)
            {
                var tracker = _context.Attach(entity);
                tracker.State = EntityState.Modified;
            }
        }

        public bool Remove(T entity)
        {
            _context.Remove(entity);
            return true;
        }

        public void RemoveRange(List<T> entities)
        {
            _context.RemoveRange(entities);
        }

        public T GetById(int id)
        {
            return _context.Set<T>().Find(id);
        }

        public async Task<T> GetByIdAsync(int id)
        {
            return await _context.Set<T>().FindAsync(id);
        }
        public async Task<T> GetByIdAsync(int id, params string[] includes)
        {
            IQueryable<T> query = _context.Set<T>();

            // Dynamically include related entities if provided
            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }

            return await query.FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
        }

        public T GetById(string code)
        {
            return _context.Set<T>().Find(code);
        }

        public async Task<T> GetByIdAsync(string code)
        {
            return await _context.Set<T>().FindAsync(code);
        }

        public T GetById(Guid code)
        {
            return _context.Set<T>().Find(code);
        }

        public async Task<T> GetByIdAsync(Guid code)
        {
            return await _context.Set<T>().FindAsync(code);
        }
    }
}