using Cutter.Data;

namespace Cutter.Services
{
    public interface IUserService
    {
        //Task<string?> GetUserRoleAsync(string userId); // ← УДАЛИ ЭТУ СТРОКУ
        Task<bool> IsInRoleAsync(string userId, string role);
        Task<Store?> GetUserStoreAsync(string userId);
    }
}
