namespace Cutter.Services
{
    public interface IStoreAccessService
    {
        Task<bool> CanUserSignInAsync(string userId);
    }
}
