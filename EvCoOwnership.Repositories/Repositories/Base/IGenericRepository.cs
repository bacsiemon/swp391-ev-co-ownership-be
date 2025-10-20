using EvCoOwnership.Helpers.BaseClasses;
using System;
using System.Linq;

namespace EvCoOwnership.Repositories.Repositories.Base
{
    public interface IGenericRepository<T> where T : class
    {
        void Create(T entity);
        void CreateRange(List<T> entities);
        Task<T> AddAsync(T entity);
        Task AddRangeAsync(List<T> entities);
        List<T> GetAll();
        Task<List<T>> GetAllAsync();
        Task<List<T>> GetAllAsync(params string[] includeProperties);
        T GetById(Guid code);
        T GetById(int id);
        T GetById(string code);
        Task<T> GetByIdAsync(Guid code);
        Task<T> GetByIdAsync(int id);
        Task<T> GetByIdAsync(int id, params string[] includes);
        Task<T> GetByIdAsync(string code);
        Task<PaginatedList<T>> GetPaginatedAsync(int page, int size, int firstPage = 1, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, params string[] includeProperties);
        bool Remove(T entity);
        void RemoveRange(List<T> entities);
        Task<bool> DeleteAsync(T entity);
        void Update(T entity);
        Task<T> UpdateAsync(T entity);
        void UpdateRange(List<T> entities);
    }
}