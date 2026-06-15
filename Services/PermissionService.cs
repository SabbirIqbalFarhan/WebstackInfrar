using Microsoft.EntityFrameworkCore;
using WebstackInfrar.Data;

namespace WebstackInfrar.Services
{
    public interface IPermissionService
    {
        Task<bool> HasPermissionAsync(string userId, string featureName);
        Task<List<string>> GetUserPermissionsAsync(string userId);
    }

    public class PermissionService : IPermissionService
    {
        private readonly ApplicationDbContext _db;
        public PermissionService(ApplicationDbContext db) => _db = db;

        public async Task<bool> HasPermissionAsync(string userId, string featureName)
        {
            return await _db.UserPermissions
                .Include(up => up.Permission)
                .AnyAsync(up => up.UserId == userId && up.Permission.FeatureName == featureName);
        }

        public async Task<List<string>> GetUserPermissionsAsync(string userId)
        {
            return await _db.UserPermissions
                .Where(up => up.UserId == userId)
                .Include(up => up.Permission)
                .Select(up => up.Permission.FeatureName)
                .ToListAsync();
        }
    }
}