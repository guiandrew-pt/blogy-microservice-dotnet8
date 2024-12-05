using UserService.Domain.Models;

namespace UserService.Repository.Repositories.Interfaces
{
	public interface IUserRepository
	{
        Task<(IEnumerable<User> Users, int TotalCount)> GetUsersAsync(string? search = null, int page = 1, int pageSize = 10);
        Task<User?> GetUserWithProfileByIdAsync(int userId);
        Task<int> AddUserWithProfileAsync(User user);
        Task<int> UpdateUserWithProfileAsync(User user);
        Task<int> DeleteUserWithProfileAsync(int userId);
    }
}

