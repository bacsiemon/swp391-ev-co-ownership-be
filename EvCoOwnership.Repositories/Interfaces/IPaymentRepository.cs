using EvCoOwnership.Repositories.Models;
using EvCoOwnership.Repositories.Repositories.Base;

namespace EvCoOwnership.Repositories.Interfaces
{
    public interface IPaymentRepository : IGenericRepository<Payment>
    {
        /// <summary>
        /// Gets recent payments for a user
        /// </summary>
        Task<List<Payment>> GetRecentPaymentsByUserIdAsync(int userId, int count = 5);

        /// <summary>
        /// Gets total payments count for a user
        /// </summary>
        Task<int> GetPaymentsCountByUserIdAsync(int userId);

        /// <summary>
        /// Gets total amount paid by a user
        /// </summary>
        Task<decimal> GetTotalAmountPaidByUserIdAsync(int userId);
    }
}