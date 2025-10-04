using EvCoOwnership.Helpers.Helpers;
using EvCoOwnership.Repositories.Models;

namespace EvCoOwnership.Services.Services
{
    /// <summary>
    /// Wrapper class to implement IJwtUserData for User entity
    /// </summary>
    public class JwtUserDataWrapper : IJwtUserData
    {
        private readonly User _user;

        public JwtUserDataWrapper(User user)
        {
            _user = user;
        }

        public int Id => _user.Id;
        public string Email => _user.Email;
        public string FirstName => _user.FirstName;
        public string LastName => _user.LastName;
    }
}