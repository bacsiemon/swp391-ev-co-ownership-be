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

        public async Task<PaginatedList<T>> GetPaginatedAsync(int page, int size, int firstPage = 1, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, params string[] includeProperties)
        {
            if (firstPage > page)
                throw new ArgumentException($"Page ({page}) must be greater or equal than firstPage ({firstPage})");

            IQueryable<T> query = _context.Set<T>();

            // Eagerly load the related entities specified in includeProperties
            if (includeProperties != null && includeProperties.Length > 0)
            {
                foreach (var includeProperty in includeProperties)
                {
                    query = query.Include(includeProperty);
                }
            }

            // Apply ordering if provided
            if (orderBy != null)
            {
                query = orderBy(query);
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
            return _context.Set<T>().Find(id)!;
        }

        public async Task<T> GetByIdAsync(int id)
        {
            return (await _context.Set<T>().FindAsync(id))!;
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

            return (await query.FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id))!;
        }

        public T GetById(string code)
        {
            return _context.Set<T>().Find(code)!;
        }

        public async Task<T> GetByIdAsync(string code)
        {
            return (await _context.Set<T>().FindAsync(code))!;
        }

        public T GetById(Guid code)
        {
            return _context.Set<T>().Find(code)!;
        }

        public async Task<T> GetByIdAsync(Guid code)
        {
            return (await _context.Set<T>().FindAsync(code))!;
        }

        // Additional async methods required by interface
        public async Task<T> AddAsync(T entity)
        {
            await _context.Set<T>().AddAsync(entity);
            return entity;
        }

        public async Task AddRangeAsync(List<T> entities)
        {
            await _context.Set<T>().AddRangeAsync(entities);
        }

        public async Task<bool> DeleteAsync(T entity)
        {
            _context.Set<T>().Remove(entity);
            return true;
        }

        public async Task<T> UpdateAsync(T entity)
        {
            _context.Set<T>().Update(entity);
            return entity;
        }
    }
}